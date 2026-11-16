import * as THREE from 'three';
import { DealerMood, ViewportPoint } from './dealer-3d.types';
import { DealerAnimator } from './dealer-animator';
import { DealerRig, buildDealer, disposeDealer } from './model/dealer-model';

export interface DealerStageOptions {
  compact: boolean;
  reducedMotion: boolean;
}

/** Portion du modèle à cadrer (dans son repère) : du sol au sommet de la tête, mains comprises. */
interface Framing {
  bottom: number;
  top: number;
  halfWidth: number;
}

const FULL_FRAMING: Framing = { bottom: -0.02, top: 2.08, halfWidth: 1.05 };
/** Sur téléphone on coupe le pied des toilettes : le personnage est plus grand à l'écran. */
const COMPACT_FRAMING: Framing = { bottom: 0.42, top: 2.08, halfWidth: 1.05 };

const FIELD_OF_VIEW = 28;
const MAX_PIXEL_RATIO = 2;
const MAX_PIXEL_RATIO_COMPACT = 1.5;
/** Sur téléphone, 30 images par seconde suffisent et ménagent la batterie. */
const COMPACT_FRAME_MS = 1000 / 30;
/** Au-delà, l'onglet revenait d'une pause : on ne rattrape pas le temps perdu d'un coup. */
const MAX_STEP_S = 0.05;

/**
 * Scène three.js du croupier : rendu, caméra, lumières et boucle d'animation.
 * Chargée à la demande par le composant, pour garder three.js hors du bundle initial.
 */
export class DealerStage {
  private readonly renderer: THREE.WebGLRenderer;
  private readonly scene = new THREE.Scene();
  private readonly camera = new THREE.PerspectiveCamera(FIELD_OF_VIEW, 4 / 3, 0.5, 30);
  private readonly rig: DealerRig;
  private readonly animator: DealerAnimator;
  private readonly keyLight: THREE.DirectionalLight;
  private readonly floor: THREE.Mesh;
  private readonly scratch = new THREE.Vector3();

  private compact: boolean;
  private reducedMotion: boolean;
  private running = false;
  private frameId = 0;
  private lastFrameAt = 0;
  private disposed = false;

  /**
   * @param context contexte WebGL2 déjà obtenu sur le canvas : le composant vérifie ainsi
   * la disponibilité de WebGL avant de construire la scène.
   */
  constructor(
    private readonly canvas: HTMLCanvasElement,
    context: WebGL2RenderingContext,
    options: DealerStageOptions,
  ) {
    this.compact = options.compact;
    this.reducedMotion = options.reducedMotion;

    this.renderer = new THREE.WebGLRenderer({ canvas, context, alpha: true, antialias: true });
    this.renderer.setClearColor(0x000000, 0);
    this.renderer.shadowMap.enabled = true;

    this.rig = buildDealer();
    this.animator = new DealerAnimator(this.rig);
    this.scene.add(this.rig.root);

    // Éclairage chaud de salle de jeu : ambiance douce, une lumière principale, un contre-jour doré.
    this.scene.add(new THREE.HemisphereLight(0xfffaf0, 0x7fa590, 2.0));

    this.keyLight = new THREE.DirectionalLight(0xfff0d6, 2.3);
    this.keyLight.position.set(-2.4, 4.2, 3.6);
    this.keyLight.shadow.mapSize.set(1024, 1024);
    this.keyLight.shadow.camera.left = -1.7;
    this.keyLight.shadow.camera.right = 1.7;
    this.keyLight.shadow.camera.top = 2.4;
    this.keyLight.shadow.camera.bottom = -0.6;
    this.keyLight.shadow.camera.near = 1;
    this.keyLight.shadow.camera.far = 10;
    this.keyLight.shadow.bias = -0.0006;
    this.keyLight.shadow.normalBias = 0.02;
    this.keyLight.shadow.radius = 5;
    this.scene.add(this.keyLight);

    const rimLight = new THREE.DirectionalLight(0xd9b45f, 0.7);
    rimLight.position.set(2.5, 2.2, -3);
    this.scene.add(rimLight);

    // Sol invisible qui ne montre que l'ombre portée : le croupier est posé, pas découpé.
    this.floor = new THREE.Mesh(new THREE.PlaneGeometry(5, 4), new THREE.ShadowMaterial({ opacity: 0.24 }));
    this.floor.rotation.x = -Math.PI / 2;
    this.floor.receiveShadow = true;
    this.scene.add(this.floor);

    canvas.addEventListener('webglcontextlost', this.onContextLost);
    canvas.addEventListener('webglcontextrestored', this.onContextRestored);

    this.applyQuality();
    // Pose le modèle avant la première image : figé, ou prêt à sortir de la cuvette.
    if (this.reducedMotion) {
      this.animator.poseStill();
    } else {
      this.animator.update(0);
    }
  }

  /** Adapte le rendu à la taille (en pixels CSS) que le parent donne au composant. */
  resize(width: number, height: number): void {
    if (width <= 0 || height <= 0) {
      return;
    }
    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / height;
    this.fitCamera();
    this.renderOnce();
  }

  setCompact(compact: boolean): void {
    if (compact === this.compact) {
      return;
    }
    this.compact = compact;
    this.applyQuality();
    this.fitCamera();
    this.renderOnce();
  }

  setMood(mood: DealerMood): void {
    this.animator.setMood(mood);
    if (this.reducedMotion) {
      this.animator.poseStill();
      this.renderOnce();
    }
  }

  setReducedMotion(reducedMotion: boolean): void {
    if (reducedMotion === this.reducedMotion) {
      return;
    }
    this.reducedMotion = reducedMotion;
    if (reducedMotion) {
      this.animator.poseStill();
      this.renderOnce();
    }
    this.syncLoop();
  }

  /** À couper quand le composant n'est pas visible : la boucle de rendu s'arrête. */
  setRunning(running: boolean): void {
    this.running = running;
    this.syncLoop();
  }

  dealTo(direction: number): void {
    // Mouvements réduits : pas de geste, mais on retient la direction pour le point de départ des cartes.
    this.animator.dealTo(direction);
    if (this.reducedMotion) {
      this.animator.poseStill();
    }
  }

  /** Point de l'écran (px, repère du viewport) où la carte quitte la main du croupier. */
  releasePoint(): ViewportPoint {
    this.camera.updateMatrixWorld();
    const projected = this.animator.releasePosition(this.scratch).project(this.camera);
    const rect = this.canvas.getBoundingClientRect();
    return {
      x: rect.left + ((projected.x + 1) / 2) * rect.width,
      y: rect.top + ((1 - projected.y) / 2) * rect.height,
    };
  }

  dispose(): void {
    this.disposed = true;
    cancelAnimationFrame(this.frameId);
    this.canvas.removeEventListener('webglcontextlost', this.onContextLost);
    this.canvas.removeEventListener('webglcontextrestored', this.onContextRestored);

    disposeDealer(this.rig);
    this.floor.geometry.dispose();
    (this.floor.material as THREE.Material).dispose();
    this.keyLight.shadow.map?.dispose();
    this.renderer.dispose();
    // Rend tout de suite le contexte au navigateur, qui n'en autorise qu'un petit nombre.
    this.renderer.forceContextLoss();
  }

  private applyQuality(): void {
    const cap = this.compact ? MAX_PIXEL_RATIO_COMPACT : MAX_PIXEL_RATIO;
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, cap));
    // Les ombres portées coûtent une passe de rendu : on s'en passe en version téléphone.
    this.keyLight.castShadow = !this.compact;
    this.floor.visible = !this.compact;
  }

  /** Recule la caméra juste assez pour que le cadrage tienne en hauteur comme en largeur. */
  private fitCamera(): void {
    const framing = this.compact ? COMPACT_FRAMING : FULL_FRAMING;
    const tan = Math.tan(THREE.MathUtils.degToRad(FIELD_OF_VIEW / 2));
    const height = framing.top - framing.bottom;
    const distance = Math.max(height / 2 / tan, framing.halfWidth / (tan * this.camera.aspect));
    const centerY = (framing.top + framing.bottom) / 2;

    // Les mains sont devant le personnage : on vise un plan un peu avancé, vu légèrement d'en haut.
    this.camera.position.set(0, centerY + 0.4, distance + 0.55);
    this.camera.lookAt(0, centerY, 0);
    this.camera.updateProjectionMatrix();
  }

  private syncLoop(): void {
    const shouldLoop = this.running && !this.reducedMotion && !this.disposed;
    if (shouldLoop && this.frameId === 0) {
      this.lastFrameAt = performance.now();
      this.frameId = requestAnimationFrame(this.onFrame);
    } else if (!shouldLoop && this.frameId !== 0) {
      cancelAnimationFrame(this.frameId);
      this.frameId = 0;
    }
  }

  private readonly onFrame = (now: number): void => {
    this.frameId = requestAnimationFrame(this.onFrame);

    const elapsedMs = now - this.lastFrameAt;
    if (this.compact && elapsedMs < COMPACT_FRAME_MS - 2) {
      return;
    }
    this.lastFrameAt = now;

    this.animator.update(Math.min(elapsedMs / 1000, MAX_STEP_S));
    this.renderer.render(this.scene, this.camera);
  };

  private renderOnce(): void {
    if (!this.disposed) {
      this.renderer.render(this.scene, this.camera);
    }
  }

  private readonly onContextLost = (event: Event): void => {
    event.preventDefault();
    cancelAnimationFrame(this.frameId);
    this.frameId = 0;
  };

  private readonly onContextRestored = (): void => {
    this.syncLoop();
    this.renderOnce();
  };
}
