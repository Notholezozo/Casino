import * as THREE from 'three';

/** Couleurs du design system Grand Salon, plus les teintes propres au personnage. */
export const COLORS = {
  porcelain: 0xffffff,
  ivory: 0xefe4cc,
  felt: 0x17543a,
  burgundy: 0x8e1f2e,
  gold: 0xd9b45f,
  black: 0x17140f,
  skin: 0xe8b98f,
  skinShade: 0xd99f78,
  mouth: 0x3b0d15,
  teeth: 0xf7f2e6,
  toothGap: 0xb7ad98,
} as const;

/** Matériaux partagés par toutes les pièces du modèle (un seul exemplaire de chaque). */
export interface DealerMaterials {
  porcelain: THREE.MeshStandardMaterial;
  glove: THREE.MeshStandardMaterial;
  skin: THREE.MeshStandardMaterial;
  skinShade: THREE.MeshStandardMaterial;
  dark: THREE.MeshStandardMaterial;
  eyeWhite: THREE.MeshStandardMaterial;
  burgundy: THREE.MeshStandardMaterial;
  gold: THREE.MeshStandardMaterial;
  /** Visière verte translucide du croupier. */
  visor: THREE.MeshStandardMaterial;
  mouth: THREE.MeshBasicMaterial;
  teeth: THREE.MeshBasicMaterial;
  toothGap: THREE.LineBasicMaterial;
}

function standard(color: number, roughness: number, metalness = 0): THREE.MeshStandardMaterial {
  return new THREE.MeshStandardMaterial({ color, roughness, metalness });
}

export function createMaterials(): DealerMaterials {
  const visor = standard(COLORS.felt, 0.35);
  visor.transparent = true;
  visor.opacity = 0.86;
  visor.side = THREE.DoubleSide;

  const porcelain = standard(COLORS.porcelain, 0.2);
  // L'intérieur de la cuvette et le dos de l'abattant se voient : pas de face cachée.
  porcelain.side = THREE.DoubleSide;

  return {
    porcelain,
    glove: standard(COLORS.ivory, 0.78),
    skin: standard(COLORS.skin, 0.62),
    skinShade: standard(COLORS.skinShade, 0.62),
    dark: standard(COLORS.black, 0.5),
    eyeWhite: standard(0xffffff, 0.3),
    burgundy: standard(COLORS.burgundy, 0.55),
    gold: standard(COLORS.gold, 0.35, 0.65),
    visor,
    // La bouche est dessinée à plat sur le visage : sans éclairage, elle reste lisible de loin.
    mouth: new THREE.MeshBasicMaterial({ color: COLORS.mouth, side: THREE.DoubleSide }),
    teeth: new THREE.MeshBasicMaterial({ color: COLORS.teeth, side: THREE.DoubleSide }),
    toothGap: new THREE.LineBasicMaterial({ color: COLORS.toothGap }),
  };
}

/** Maillage qui projette et reçoit les ombres (réglage commun à presque toutes les pièces). */
export function solid(geometry: THREE.BufferGeometry, material: THREE.Material): THREE.Mesh {
  const mesh = new THREE.Mesh(geometry, material);
  mesh.castShadow = true;
  mesh.receiveShadow = true;
  return mesh;
}
