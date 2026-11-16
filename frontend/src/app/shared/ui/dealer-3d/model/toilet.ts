import * as THREE from 'three';
import { RoundedBoxGeometry } from 'three/examples/jsm/geometries/RoundedBoxGeometry.js';
import { DealerMaterials, solid } from './materials';

/** Hauteur du rebord de la cuvette : le cou du croupier en sort. */
export const BOWL_RIM_Y = 0.84;
/** La cuvette est allongée vers l'avant ; son centre est décalé d'autant. */
export const BOWL_CENTER_Z = 0.06;

const BOWL_STRETCH = 1.2;

/** Toilettes en porcelaine vues de face : cuvette, lunette, abattant relevé, réservoir. */
export function buildToilet(materials: DealerMaterials): THREE.Group {
  const toilet = new THREE.Group();

  // Cuvette : un profil (rayon, hauteur) tourné autour de l'axe vertical. Il monte par l'extérieur
  // du pied jusqu'au rebord, puis redescend par l'intérieur pour creuser la cuvette.
  const profile: [number, number][] = [
    [0, 0],
    [0.3, 0],
    [0.31, 0.04],
    [0.27, 0.1],
    [0.22, 0.3],
    [0.24, 0.46],
    [0.33, 0.62],
    [0.42, 0.74],
    [0.45, 0.8],
    [0.45, BOWL_RIM_Y],
    [0.36, BOWL_RIM_Y],
    [0.33, 0.74],
    [0.25, 0.58],
    [0.12, 0.5],
    [0, 0.48],
  ];
  const bowl = solid(
    new THREE.LatheGeometry(
      profile.map(([radius, y]) => new THREE.Vector2(radius, y)),
      48,
    ),
    materials.porcelain,
  );
  bowl.scale.z = BOWL_STRETCH;
  bowl.position.z = BOWL_CENTER_Z;
  toilet.add(bowl);

  // Lunette posée sur le rebord.
  const seat = solid(new THREE.TorusGeometry(0.405, 0.042, 12, 48), materials.porcelain);
  seat.rotation.x = Math.PI / 2;
  seat.scale.set(1, BOWL_STRETCH, 0.55);
  seat.position.set(0, BOWL_RIM_Y + 0.012, BOWL_CENTER_Z);
  toilet.add(seat);

  // Abattant relevé, appuyé contre le réservoir : il fait un fond clair derrière la tête.
  const lid = solid(new THREE.CylinderGeometry(0.4, 0.4, 0.035, 48), materials.porcelain);
  lid.scale.z = BOWL_STRETCH;
  lid.rotation.x = Math.PI / 2 - 0.1;
  lid.position.set(0, BOWL_RIM_Y + 0.47, -0.4);
  toilet.add(lid);

  // Réservoir et son couvercle.
  const tank = solid(new RoundedBoxGeometry(0.98, 0.62, 0.3, 4, 0.05), materials.porcelain);
  tank.position.set(0, BOWL_RIM_Y + 0.33, -0.66);
  toilet.add(tank);

  const tankLid = solid(new RoundedBoxGeometry(1.04, 0.07, 0.36, 3, 0.03), materials.porcelain);
  tankLid.position.set(0, BOWL_RIM_Y + 0.675, -0.66);
  toilet.add(tankLid);

  // Manette de chasse d'eau dorée, sur le devant du réservoir.
  const lever = solid(new THREE.CylinderGeometry(0.016, 0.016, 0.13, 10), materials.gold);
  lever.rotation.z = Math.PI / 2 - 0.25;
  lever.position.set(-0.36, BOWL_RIM_Y + 0.54, -0.49);
  toilet.add(lever);

  const leverKnob = solid(new THREE.SphereGeometry(0.03, 14, 10), materials.gold);
  leverKnob.position.set(-0.3, BOWL_RIM_Y + 0.555, -0.49);
  toilet.add(leverKnob);

  return toilet;
}
