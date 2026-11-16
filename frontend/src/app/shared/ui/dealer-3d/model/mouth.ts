import * as THREE from 'three';
import { DealerMaterials } from './materials';

const SEGMENTS = 18;
const TEETH = 8;
const HALF_WIDTH = 0.135;
/** De combien les coins montent (sourire) ou descendent (moue) par rapport au centre. */
const CORNER_LIFT = 0.05;
const MAX_OPENING = 0.09;

/**
 * Bouche dessinée à même le visage : une bande dont on recalcule les sommets selon l'humeur.
 * Lèvre du haut = parabole dont les coins montent ou descendent ; lèvre du bas = la même, creusée au
 * centre par l'ouverture. Les dents couvrent le haut de l'ouverture.
 */
export class Mouth {
  readonly group = new THREE.Group();

  private readonly interior = new THREE.BufferGeometry();
  private readonly teeth = new THREE.BufferGeometry();
  private readonly gaps = new THREE.BufferGeometry();

  /**
   * @param surfaceZ profondeur du visage au point (x, y) : la bouche épouse la courbure de la tête.
   * @param centerY hauteur du milieu de la lèvre du haut, dans le repère de la tête.
   */
  constructor(
    materials: DealerMaterials,
    private readonly surfaceZ: (x: number, y: number) => number,
    private readonly centerY: number,
  ) {
    const stripVertices = (SEGMENTS + 1) * 2;
    const indices: number[] = [];
    for (let i = 0; i < SEGMENTS; i++) {
      const top = i * 2;
      indices.push(top, top + 1, top + 2, top + 1, top + 3, top + 2);
    }

    for (const geometry of [this.interior, this.teeth]) {
      geometry.setAttribute('position', new THREE.BufferAttribute(new Float32Array(stripVertices * 3), 3));
      geometry.setIndex(indices);
    }
    this.gaps.setAttribute(
      'position',
      new THREE.BufferAttribute(new Float32Array((TEETH - 1) * 2 * 3), 3),
    );

    this.group.add(
      new THREE.Mesh(this.interior, materials.mouth),
      new THREE.Mesh(this.teeth, materials.teeth),
      new THREE.LineSegments(this.gaps, materials.toothGap),
    );
    this.update(0.7, 0.7);
  }

  /**
   * @param smile négatif = moue (coins vers le bas), positif = sourire ; ±1 est déjà très marqué.
   * @param open 0 (fermée) … 1 (grande ouverte).
   */
  update(smile: number, open: number): void {
    const interior = this.interior.getAttribute('position') as THREE.BufferAttribute;
    const teeth = this.teeth.getAttribute('position') as THREE.BufferAttribute;
    // Une moue serre les lèvres : les dents n'apparaissent qu'avec le sourire.
    const teethShare = 0.62 * THREE.MathUtils.clamp(smile + 0.3, 0, 1);

    for (let i = 0; i <= SEGMENTS; i++) {
      const t = (i / SEGMENTS) * 2 - 1;
      const x = t * HALF_WIDTH;
      const top = this.topLip(t, smile);
      const opening = this.opening(t, open);
      const z = this.surfaceZ(x, top - opening / 2);

      interior.setXYZ(i * 2, x, top, z + 0.006);
      interior.setXYZ(i * 2 + 1, x, top - opening, z + 0.006);
      teeth.setXYZ(i * 2, x, top, z + 0.011);
      teeth.setXYZ(i * 2 + 1, x, top - opening * teethShare, z + 0.011);
    }

    const gaps = this.gaps.getAttribute('position') as THREE.BufferAttribute;
    for (let tooth = 1; tooth < TEETH; tooth++) {
      const t = (tooth / TEETH) * 2 - 1;
      const x = t * HALF_WIDTH;
      const top = this.topLip(t, smile);
      const opening = this.opening(t, open);
      const z = this.surfaceZ(x, top) + 0.015;

      gaps.setXYZ((tooth - 1) * 2, x, top, z);
      gaps.setXYZ((tooth - 1) * 2 + 1, x, top - opening * teethShare, z);
    }

    interior.needsUpdate = true;
    teeth.needsUpdate = true;
    gaps.needsUpdate = true;
  }

  private topLip(t: number, smile: number): number {
    return this.centerY + smile * CORNER_LIFT * t * t;
  }

  /** Hauteur de l'ouverture : maximale au centre, nulle aux commissures. */
  private opening(t: number, open: number): number {
    return MAX_OPENING * open * Math.pow(Math.max(0, 1 - t * t), 0.6);
  }
}
