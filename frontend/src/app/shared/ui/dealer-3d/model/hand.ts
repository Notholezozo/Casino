import * as THREE from 'three';
import { RoundedBoxGeometry } from 'three/examples/jsm/geometries/RoundedBoxGeometry.js';
import { DealerMaterials, solid } from './materials';

/** Les mains sont volontairement grosses, façon gants de dessin animé. */
export const HAND_SCALE = 1.2;
const FINGER_X = [-0.063, -0.021, 0.021, 0.063];
const FINGER_LENGTH = [0.9, 1, 1, 0.86];

export interface HandRig {
  /**
   * Repère de la main : doigts vers +Y, paume vers +Z, pouce vers ±X selon la main.
   * L'ordre de rotation est lacet (Y), tangage (X), roulis (Z) : on oriente la main comme on la décrit.
   */
  root: THREE.Group;
  /** Articulations à la base des doigts, puis au milieu : les plier referme la main. */
  knuckles: THREE.Group[];
  middleJoints: THREE.Group[];
  thumb: THREE.Group;
}

/**
 * Main gantée flottante : paume, quatre doigts à deux phalanges, pouce et manchette à bouton doré.
 * @param thumbSide côté du pouce dans le repère de la main (+1 ou -1) : distingue main droite et gauche.
 */
export function buildHand(materials: DealerMaterials, thumbSide: 1 | -1): HandRig {
  const root = new THREE.Group();
  root.rotation.order = 'YXZ';
  root.scale.setScalar(HAND_SCALE);

  const palm = solid(new RoundedBoxGeometry(0.18, 0.17, 0.066, 4, 0.03), materials.glove);
  root.add(palm);

  const knuckles: THREE.Group[] = [];
  const middleJoints: THREE.Group[] = [];
  const lowerGeometry = new THREE.CapsuleGeometry(0.0215, 0.05, 4, 10);
  const upperGeometry = new THREE.CapsuleGeometry(0.0195, 0.04, 4, 10);

  FINGER_X.forEach((x, index) => {
    const knuckle = new THREE.Group();
    knuckle.position.set(x, 0.08, 0);
    knuckle.scale.setScalar(FINGER_LENGTH[index] ?? 1);

    const lower = solid(lowerGeometry, materials.glove);
    lower.position.y = 0.04;
    knuckle.add(lower);

    const middle = new THREE.Group();
    middle.position.y = 0.082;
    const upper = solid(upperGeometry, materials.glove);
    upper.position.y = 0.034;
    middle.add(upper);
    knuckle.add(middle);

    root.add(knuckle);
    knuckles.push(knuckle);
    middleJoints.push(middle);
  });

  const thumb = new THREE.Group();
  thumb.position.set(thumbSide * 0.082, -0.03, 0.012);
  thumb.rotation.z = -thumbSide * 0.95;
  const thumbTip = solid(new THREE.CapsuleGeometry(0.0245, 0.06, 4, 10), materials.glove);
  thumbTip.position.y = 0.045;
  thumb.add(thumbTip);
  root.add(thumb);

  // Manchette évasée, fermée par un bouton doré sur le dos de la main.
  const cuff = solid(new THREE.CylinderGeometry(0.078, 0.092, 0.075, 20), materials.glove);
  cuff.position.y = -0.118;
  cuff.scale.z = 0.62;
  root.add(cuff);

  const cuffTrim = solid(new THREE.TorusGeometry(0.09, 0.009, 8, 24), materials.gold);
  cuffTrim.rotation.x = Math.PI / 2;
  cuffTrim.scale.y = 0.62;
  cuffTrim.position.y = -0.155;
  root.add(cuffTrim);

  const button = solid(new THREE.SphereGeometry(0.014, 10, 8), materials.gold);
  button.position.set(0, -0.112, -0.052);
  root.add(button);

  return { root, knuckles, middleJoints, thumb };
}

/** Referme les doigts vers la paume : 0 = main à plat, 1 = poing presque fermé. */
export function curlFingers(hand: HandRig, curl: number): void {
  for (const knuckle of hand.knuckles) {
    knuckle.rotation.x = curl * 1.1;
  }
  for (const joint of hand.middleJoints) {
    joint.rotation.x = curl * 1.3;
  }
}
