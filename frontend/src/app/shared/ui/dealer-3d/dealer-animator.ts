import * as THREE from 'three';
import { DEAL_RELEASE_MS, DealerMood } from './dealer-3d.types';
import { DealerRig } from './model/dealer-model';
import { HAND_SCALE, HandRig, curlFingers } from './model/hand';

// ---------------------------------------------------------------- Poses des mains

/** Position et orientation d'une main dans le repère du modèle, plus la fermeture des doigts. */
interface HandPose {
  x: number;
  y: number;
  z: number;
  /** Tangage : π/2 = paume vers le bas, doigts vers le spectateur. */
  pitch: number;
  /** Lacet : positif = les doigts tournent vers la droite de l'écran. */
  yaw: number;
  roll: number;
  curl: number;
}

const HALF_PI = Math.PI / 2;

const DEALING_REST: HandPose = { x: -0.52, y: 0.9, z: 0.7, pitch: HALF_PI - 0.4, yaw: 0.4, roll: 0, curl: 0.3 };
/** Au-dessus du paquet, doigts refermés sur la carte du dessus. */
const DEALING_PICK: HandPose = { x: 0.32, y: 1.08, z: 0.82, pitch: HALF_PI - 0.1, yaw: 1.0, roll: 0, curl: 0.5 };
/** Paume vers le haut, inclinée vers le spectateur pour qu'on voie le dos du paquet. */
const DECK_REST: HandPose = { x: 0.52, y: 0.88, z: 0.72, pitch: -HALF_PI - 0.35, yaw: Math.PI - 0.3, roll: 0, curl: 0.22 };

/** Bras tendu vers la cible : direction -1 = gauche de l'écran, 1 = droite. */
function throwPose(direction: number): HandPose {
  return {
    x: -0.2 + 0.5 * direction,
    y: 0.8,
    z: 1.04,
    pitch: HALF_PI + 0.22,
    yaw: 0.75 * direction,
    roll: 0,
    curl: 0,
  };
}

function mixPose(from: HandPose, to: HandPose, t: number): HandPose {
  const mix = (a: number, b: number) => a + (b - a) * t;
  return {
    x: mix(from.x, to.x),
    y: mix(from.y, to.y),
    z: mix(from.z, to.z),
    pitch: mix(from.pitch, to.pitch),
    yaw: mix(from.yaw, to.yaw),
    roll: mix(from.roll, to.roll),
    curl: mix(from.curl, to.curl),
  };
}

function applyPose(hand: HandRig, pose: HandPose): void {
  hand.root.position.set(pose.x, pose.y, pose.z);
  hand.root.rotation.set(pose.pitch, pose.yaw, pose.roll);
  curlFingers(hand, pose.curl);
}

// ---------------------------------------------------------------- Geste de distribution

/** Étapes du geste, en ms depuis dealTo(). La carte part à DEAL_RELEASE_MS. */
const PICK_END_MS = 65;
const FOLLOW_END_MS = 240;
const GESTURE_END_MS = 560;
const CARD_FLIGHT_MS = 160;
const CARD_SPEED = 4.4;

interface Gesture {
  elapsedMs: number;
  direction: number;
  /** Pose de la main au moment de l'appel : un geste peut en interrompre un autre sans saut. */
  from: HandPose;
  released: boolean;
}

interface FlyingCard {
  card: THREE.Group;
  ageMs: number;
  velocity: THREE.Vector3;
}

// ---------------------------------------------------------------- Expressions

interface Expression {
  /** Négatif = moue, positif = sourire (voir Mouth.update). */
  smile: number;
  open: number;
  browRaise: number;
  /** Positif = sourcils en accent circonflexe (inquiet) ; négatif = froncés, malicieux. */
  browTilt: number;
  /** Positif = tête penchée vers le bas. */
  headPitch: number;
  eyeOpen: number;
  /** Intensité du petit rebond de joie. */
  bounce: number;
}

const EXPRESSIONS: Record<DealerMood, Expression> = {
  idle: { smile: 0.55, open: 0.55, browRaise: 0, browTilt: -0.12, headPitch: 0, eyeOpen: 1, bounce: 0 },
  happy: { smile: 1.15, open: 1.15, browRaise: 0.02, browTilt: -0.02, headPitch: -0.06, eyeOpen: 1.06, bounce: 1 },
  sad: { smile: -1.4, open: 0.18, browRaise: -0.004, browTilt: 0.5, headPitch: 0.26, eyeOpen: 0.62, bounce: 0 },
};

/** Durée de la sortie de la cuvette à l'apparition. */
const EMERGE_S = 0.9;
/** De combien le personnage est enfoncé dans la cuvette avant d'en sortir. */
const EMERGE_DEPTH = 0.85;

const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);
const easeInCubic = (t: number) => t * t * t;
const easeInOut = (t: number) => t * t * (3 - 2 * t);
const easeOutBack = (t: number) => 1 + 2.2 * Math.pow(t - 1, 3) + 1.2 * Math.pow(t - 1, 2);
const clamp01 = (t: number) => Math.min(1, Math.max(0, t));

/**
 * Anime le croupier : respiration et clignements au repos, expression selon l'humeur,
 * et geste de distribution. Ne connaît ni le rendu ni Angular : on lui donne le temps écoulé.
 */
export class DealerAnimator {
  private time = 0;
  private emerge = 0;
  private readonly expression: Expression = { ...EXPRESSIONS.idle };
  private target: Expression = EXPRESSIONS.idle;

  private gesture: Gesture | null = null;
  private dealingPose: HandPose = DEALING_REST;
  private readonly flying: FlyingCard[] = [];
  private nextFlyingCard = 0;
  /** Direction du dernier lancer : la tête et le point de départ des cartes la suivent. */
  private lastDirection = 0;
  private glance = 0;
  /** Petit sursaut de la main au paquet à chaque carte tirée (1 → 0). */
  private deckKick = 0;

  private nextBlinkAt = 1.6;
  private blinkElapsed = -1;

  constructor(private readonly rig: DealerRig) {}

  setMood(mood: DealerMood): void {
    this.target = EXPRESSIONS[mood];
  }

  /** Lance un geste de distribution ; un geste en cours est repris depuis la pose actuelle. */
  dealTo(direction: number): void {
    this.lastDirection = direction;
    this.gesture = { elapsedMs: 0, direction, from: this.dealingPose, released: false };
    this.deckKick = 1;
  }

  /** Point (repère du modèle) où la carte quitte la main, pour la direction du dernier lancer. */
  releasePosition(out: THREE.Vector3): THREE.Vector3 {
    const pose = throwPose(this.lastDirection);
    return out.set(pose.x + Math.sin(pose.yaw) * 0.16, pose.y - 0.04, pose.z + Math.cos(pose.yaw) * 0.16);
  }

  /** Avance l'animation de `dt` secondes. */
  update(dt: number): void {
    this.time += dt;
    this.emerge = Math.min(1, this.emerge + dt / EMERGE_S);
    this.easeExpression(1 - Math.exp(-dt * 7));
    this.updateBlink(dt);
    this.updateGesture(dt);
    this.updateFlyingCards(dt);
    this.deckKick *= Math.exp(-dt * 14);
    this.apply(true);
  }

  /** Pose figée (mouvements réduits) : expression de l'humeur courante, mains au repos, sans geste. */
  poseStill(): void {
    this.emerge = 1;
    this.gesture = null;
    this.dealingPose = DEALING_REST;
    this.rig.heldCard.visible = false;
    for (const flying of this.flying.splice(0)) {
      flying.card.visible = false;
    }
    this.blinkElapsed = -1;
    this.glance = 0;
    this.deckKick = 0;
    this.easeExpression(1);
    this.apply(false);
  }

  private easeExpression(amount: number): void {
    const current = this.expression;
    for (const key of Object.keys(current) as (keyof Expression)[]) {
      current[key] += (this.target[key] - current[key]) * amount;
    }
  }

  private updateBlink(dt: number): void {
    if (this.blinkElapsed >= 0) {
      this.blinkElapsed += dt;
      if (this.blinkElapsed > 0.16) {
        this.blinkElapsed = -1;
        this.nextBlinkAt = this.time + 2.2 + Math.random() * 3;
      }
    } else if (this.time >= this.nextBlinkAt) {
      this.blinkElapsed = 0;
    }
  }

  private updateGesture(dt: number): void {
    const gesture = this.gesture;
    if (!gesture) {
      this.dealingPose = DEALING_REST;
      this.glance += (0 - this.glance) * (1 - Math.exp(-dt * 5));
      return;
    }

    gesture.elapsedMs += dt * 1000;
    const ms = gesture.elapsedMs;
    const aim = throwPose(gesture.direction);

    if (ms < PICK_END_MS) {
      // 1. La main file au paquet.
      this.dealingPose = mixPose(gesture.from, DEALING_PICK, easeOutCubic(ms / PICK_END_MS));
    } else if (ms < DEAL_RELEASE_MS) {
      // 2. Elle part vers la cible en accélérant, la carte sous les doigts.
      const t = (ms - PICK_END_MS) / (DEAL_RELEASE_MS - PICK_END_MS);
      this.dealingPose = mixPose(DEALING_PICK, aim, easeInCubic(t));
    } else if (ms < FOLLOW_END_MS) {
      // 3. La carte est partie : la main reste tendue un instant.
      this.dealingPose = aim;
    } else {
      // 4. Retour au repos.
      const t = clamp01((ms - FOLLOW_END_MS) / (GESTURE_END_MS - FOLLOW_END_MS));
      this.dealingPose = mixPose(aim, DEALING_REST, easeInOut(t));
    }

    this.rig.heldCard.visible = ms >= PICK_END_MS * 0.6 && !gesture.released;

    if (!gesture.released && ms >= DEAL_RELEASE_MS) {
      gesture.released = true;
      this.rig.heldCard.visible = false;
      this.launchCard(gesture.direction, aim);
    }

    this.glance += (gesture.direction * 0.32 - this.glance) * (1 - Math.exp(-dt * 12));

    if (ms >= GESTURE_END_MS) {
      this.gesture = null;
    }
  }

  /** Fait partir une carte 3D de la main ; elle disparaît vite, la carte de la page prend le relais. */
  private launchCard(direction: number, aim: HandPose): void {
    const card = this.rig.flyingCards[this.nextFlyingCard];
    if (!card) {
      return;
    }
    this.nextFlyingCard = (this.nextFlyingCard + 1) % this.rig.flyingCards.length;

    // La carte était peut-être encore en vol (lancers très rapprochés) : on la reprend.
    const index = this.flying.findIndex((flying) => flying.card === card);
    if (index >= 0) {
      this.flying.splice(index, 1);
    }

    card.position.set(aim.x + Math.sin(aim.yaw) * 0.16, aim.y - 0.04, aim.z + Math.cos(aim.yaw) * 0.16);
    // À plat, dos vers le haut, dans l'axe du lancer.
    card.rotation.set(-HALF_PI, 0, -aim.yaw);
    card.scale.setScalar(1);
    card.visible = true;

    const velocity = new THREE.Vector3(direction * 1.15, -0.3, 1).normalize().multiplyScalar(CARD_SPEED);
    this.flying.push({ card, ageMs: 0, velocity });
  }

  private updateFlyingCards(dt: number): void {
    for (let i = this.flying.length - 1; i >= 0; i--) {
      const flying = this.flying[i];
      if (!flying) {
        continue;
      }
      flying.ageMs += dt * 1000;
      const progress = flying.ageMs / CARD_FLIGHT_MS;

      if (progress >= 1) {
        flying.card.visible = false;
        this.flying.splice(i, 1);
        continue;
      }

      flying.card.position.addScaledVector(flying.velocity, dt);
      flying.card.rotation.z += dt * 14;
      // Elle s'efface en rétrécissant sur la fin du vol.
      flying.card.scale.setScalar(1 - easeInCubic(progress) * 0.85);
    }
  }

  /** Reporte l'état courant sur le modèle. `alive` = avec les micro-mouvements de repos. */
  private apply(alive: boolean): void {
    const { character, dealingHand, deckHand } = this.rig;
    const face = this.expression;
    const t = alive ? this.time : 0;
    const wave = (speed: number, phase = 0) => (alive ? Math.sin(t * speed + phase) : 0);

    // Sortie de la cuvette, respiration, rebond de joie.
    const emerged = easeOutBack(this.emerge);
    const bounce = face.bounce * Math.abs(wave(8.5)) * 0.035;
    character.root.position.y = (emerged - 1) * EMERGE_DEPTH + wave(1.6) * 0.012 + bounce;
    character.root.rotation.z = -this.glance * 0.12;

    const head = character.head;
    head.pivot.rotation.set(
      face.headPitch + wave(0.8) * 0.02,
      wave(0.5) * 0.07 + this.glance,
      wave(0.37, 1) * 0.035 + face.bounce * wave(8.5, 0.6) * 0.05,
    );

    head.mouth.update(face.smile, face.open);

    const blink = this.blinkElapsed < 0 ? 1 : 0.1 + 0.9 * Math.abs(this.blinkElapsed / 0.08 - 1);
    for (const eye of head.eyes) {
      eye.scale.y = Math.min(face.eyeOpen, 1.1) * clamp01(blink);
    }

    head.brows.forEach((brow, index) => {
      // Le sourcil de gauche et celui de droite s'inclinent en miroir.
      const side = index === 0 ? 1 : -1;
      brow.position.y = head.browRestY + face.browRaise;
      brow.rotation.z = side * face.browTilt;
    });

    // Mains : elles apparaissent en grossissant, puis flottent doucement.
    const handScale = HAND_SCALE * clamp01(emerged);
    const calm = this.gesture ? 0.2 : 1;

    applyPose(dealingHand, this.dealingPose);
    dealingHand.root.position.y += wave(1.7) * 0.014 * calm;
    dealingHand.root.rotation.z += wave(1.1, 0.5) * 0.04 * calm;
    dealingHand.root.scale.setScalar(handScale);

    applyPose(deckHand, DECK_REST);
    deckHand.root.position.y += wave(1.7, 2.1) * 0.014 - this.deckKick * 0.02;
    deckHand.root.rotation.z += wave(1.1, 2.6) * 0.03;
    deckHand.root.scale.setScalar(handScale);
    // Le pouce pousse la carte du dessus à chaque lancer.
    deckHand.thumb.rotation.x = 0.5 + this.deckKick * 0.35;
  }
}
