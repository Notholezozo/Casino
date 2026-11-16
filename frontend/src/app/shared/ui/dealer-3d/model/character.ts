import * as THREE from 'three';
import { DealerMaterials, solid } from './materials';
import { Mouth } from './mouth';
import { BOWL_CENTER_Z } from './toilet';

// Repère de la tête : origine au sommet du cou (le pivot du hochement), +Z vers le spectateur.
const HEAD_CENTER_Y = 0.27;
const HEAD_RADIUS = { x: 0.3, y: 0.34, z: 0.3 };

const NECK_BOTTOM_Y = 0.5;
/** Hauteur du sommet du cou, donc du pivot de la tête, dans le repère du personnage. */
const NECK_TOP_Y = 1.3;

export interface HeadRig {
  /** Pivot au sommet du cou : tourner ce groupe fait hocher ou pencher la tête. */
  pivot: THREE.Group;
  /** Un groupe par œil ; écraser son échelle verticale ferme la paupière. */
  eyes: THREE.Group[];
  /** [sourcil à gauche de l'écran, sourcil à droite] ; position de repos dans `browRestY`. */
  brows: THREE.Group[];
  browRestY: number;
  mouth: Mouth;
}

export interface CharacterRig {
  /** Cou, col, nœud papillon et tête : ce groupe monte et descend dans la cuvette. */
  root: THREE.Group;
  head: HeadRig;
}

/** Profondeur de la surface du crâne (ellipsoïde) au point (x, y) du visage. */
function faceZ(x: number, y: number): number {
  const nx = x / HEAD_RADIUS.x;
  const ny = (y - HEAD_CENTER_Y) / HEAD_RADIUS.y;
  return HEAD_RADIUS.z * Math.sqrt(Math.max(0.0001, 1 - nx * nx - ny * ny));
}

/**
 * Le croupier : une tête cartoon originale (visière verte, fine moustache, grand sourire)
 * au bout d'un long cou qui sort de la cuvette, avec col et nœud papillon.
 */
export function buildCharacter(materials: DealerMaterials): CharacterRig {
  const root = new THREE.Group();
  root.position.z = BOWL_CENTER_Z;

  const neckHeight = NECK_TOP_Y - NECK_BOTTOM_Y;
  const neck = solid(new THREE.CylinderGeometry(0.1, 0.125, neckHeight, 24), materials.skin);
  neck.position.y = NECK_BOTTOM_Y + neckHeight / 2;
  root.add(neck);

  root.add(buildCollar(materials));

  const head = buildHead(materials);
  head.pivot.position.y = NECK_TOP_Y;
  root.add(head.pivot);

  return { root, head };
}

function buildCollar(materials: DealerMaterials): THREE.Group {
  const collar = new THREE.Group();
  collar.position.y = 1.07;

  const band = solid(new THREE.TorusGeometry(0.118, 0.03, 10, 28), materials.glove);
  band.rotation.x = Math.PI / 2;
  collar.add(band);

  // Nœud papillon : deux cônes aplatis dont la pointe se rejoint sous le nœud central.
  const wingGeometry = new THREE.ConeGeometry(0.075, 0.15, 18);
  for (const side of [-1, 1]) {
    const wing = solid(wingGeometry, materials.burgundy);
    wing.scale.z = 0.45;
    wing.rotation.z = (side * Math.PI) / 2;
    wing.position.set(side * 0.08, 0, 0.15);
    collar.add(wing);
  }
  const knot = solid(new THREE.SphereGeometry(0.036, 14, 10), materials.burgundy);
  knot.position.set(0, 0, 0.155);
  collar.add(knot);

  return collar;
}

function buildHead(materials: DealerMaterials): HeadRig {
  const pivot = new THREE.Group();

  const skull = solid(new THREE.SphereGeometry(1, 40, 28), materials.skin);
  skull.scale.set(HEAD_RADIUS.x, HEAD_RADIUS.y, HEAD_RADIUS.z);
  skull.position.y = HEAD_CENTER_Y;
  pivot.add(skull);

  const earGeometry = new THREE.SphereGeometry(0.062, 14, 10);
  for (const side of [-1, 1]) {
    const ear = solid(earGeometry, materials.skinShade);
    ear.scale.set(0.45, 1, 0.75);
    ear.position.set(side * (HEAD_RADIUS.x - 0.004), HEAD_CENTER_Y + 0.02, 0);
    pivot.add(ear);
  }

  // Cheveux : une calotte basculée vers l'arrière, pour dégager le front sous la visière.
  // La calotte est basculée AVANT d'être étirée aux dimensions du crâne, pour rester partout au-dessus de lui.
  const hair = new THREE.Group();
  hair.scale.set(HEAD_RADIUS.x + 0.014, HEAD_RADIUS.y + 0.014, HEAD_RADIUS.z + 0.014);
  hair.position.y = HEAD_CENTER_Y;
  const hairCap = solid(new THREE.SphereGeometry(1, 32, 14, 0, Math.PI * 2, 0, 1.25), materials.dark);
  hairCap.rotation.x = -0.45;
  hair.add(hairCap);
  pivot.add(hair);

  pivot.add(buildVisor(materials));

  const eyes = [-1, 1].map((side) => buildEye(materials, side));
  const browRestY = HEAD_CENTER_Y + 0.165;
  const brows = [-1, 1].map((side) => buildBrow(materials, side, browRestY));
  pivot.add(...eyes, ...brows);

  const noseY = HEAD_CENTER_Y - 0.005;
  const nose = solid(new THREE.SphereGeometry(0.052, 16, 12), materials.skinShade);
  nose.position.set(0, noseY, faceZ(0, noseY) + 0.03);
  pivot.add(nose);

  pivot.add(...[-1, 1].map((side) => buildMoustacheHalf(materials, side)));

  const mouth = new Mouth(materials, faceZ, HEAD_CENTER_Y - 0.165);
  pivot.add(mouth.group);

  return { pivot, eyes, brows, browRestY, mouth };
}

/** Visière de croupier : un bandeau autour du front et une casquette verte translucide. */
function buildVisor(materials: DealerMaterials): THREE.Group {
  const visor = new THREE.Group();
  const bandOffset = 0.23;
  visor.position.y = HEAD_CENTER_Y + bandOffset;
  visor.rotation.x = 0.06;

  const headRadiusAtBand =
    HEAD_RADIUS.x * Math.sqrt(1 - (bandOffset / HEAD_RADIUS.y) * (bandOffset / HEAD_RADIUS.y));
  const bandRadius = headRadiusAtBand + 0.012;

  const band = solid(new THREE.TorusGeometry(bandRadius, 0.024, 10, 40), materials.dark);
  band.rotation.x = Math.PI / 2;
  visor.add(band);

  // Casquette : un croissant, bord intérieur collé au bandeau, bord extérieur en ellipse vers l'avant.
  const reach = 0.24;
  const steps = 28;
  const outline = new THREE.Shape();
  const rim: THREE.Vector3[] = [];
  for (let i = 0; i <= steps; i++) {
    const angle = (i / steps) * Math.PI;
    const x = Math.cos(angle) * (bandRadius + 0.03);
    const y = Math.sin(angle) * (bandRadius + reach);
    if (i === 0) {
      outline.moveTo(x, y);
    } else {
      outline.lineTo(x, y);
    }
    rim.push(new THREE.Vector3(x, y, 0));
  }
  for (let i = steps; i >= 0; i--) {
    const angle = (i / steps) * Math.PI;
    outline.lineTo(Math.cos(angle) * bandRadius, Math.sin(angle) * bandRadius);
  }

  const brim = new THREE.Group();
  // Le croissant est dessiné à plat (XY) : on le couche vers l'avant, relevé pour dégager le regard.
  brim.rotation.x = Math.PI / 2 - 0.22;

  const brimMesh = new THREE.Mesh(new THREE.ShapeGeometry(outline), materials.visor);
  brimMesh.castShadow = true;
  brim.add(brimMesh);

  // Liseré doré sur le bord extérieur.
  const edge = solid(
    new THREE.TubeGeometry(new THREE.CatmullRomCurve3(rim), 40, 0.008, 6),
    materials.gold,
  );
  brim.add(edge);
  visor.add(brim);

  return visor;
}

function buildEye(materials: DealerMaterials, side: number): THREE.Group {
  const eye = new THREE.Group();
  const x = side * 0.108;
  const y = HEAD_CENTER_Y + 0.06;
  eye.position.set(x, y, faceZ(x, y) - 0.022);

  const white = new THREE.Mesh(new THREE.SphereGeometry(0.064, 18, 14), materials.eyeWhite);
  white.scale.set(1, 1.15, 0.75);
  eye.add(white);

  // Pupilles légèrement rapprochées : un regard un peu trop appuyé.
  const pupil = new THREE.Mesh(new THREE.SphereGeometry(0.027, 14, 10), materials.dark);
  pupil.position.set(-side * 0.012, -0.004, 0.036);
  eye.add(pupil);

  const glint = new THREE.Mesh(new THREE.SphereGeometry(0.008, 8, 6), materials.eyeWhite);
  glint.position.set(-side * 0.012 + 0.01, 0.008, 0.06);
  eye.add(glint);

  return eye;
}

function buildBrow(materials: DealerMaterials, side: number, y: number): THREE.Group {
  const brow = new THREE.Group();
  const x = side * 0.11;
  brow.position.set(x, y, faceZ(x, y) + 0.012);
  // Suit la courbure du front : l'extrémité extérieure part vers l'arrière.
  brow.rotation.y = side * 0.38;

  const bar = solid(new THREE.CapsuleGeometry(0.017, 0.085, 4, 10), materials.dark);
  bar.rotation.z = Math.PI / 2;
  brow.add(bar);

  return brow;
}

/** Fine moustache en guidon : deux pointes effilées qui remontent vers l'extérieur. */
function buildMoustacheHalf(materials: DealerMaterials, side: number): THREE.Mesh {
  const half = solid(new THREE.ConeGeometry(0.014, 0.19, 12), materials.dark);
  const x = side * 0.098;
  const y = HEAD_CENTER_Y - 0.078;
  half.position.set(x, y, faceZ(x, y) + 0.012);
  half.scale.z = 0.7;
  half.rotation.set(0, side * 0.36, -side * (Math.PI / 2 - 0.2));
  return half;
}
