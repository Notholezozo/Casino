import * as THREE from 'three';
import { CardFactory } from './cards';
import { CharacterRig, buildCharacter } from './character';
import { HandRig, buildHand } from './hand';
import { createMaterials } from './materials';
import { buildToilet } from './toilet';

/** Nombre de cartes pouvant être en vol en même temps (elles ne vivent que ~150 ms). */
const FLYING_CARDS = 4;

/** Le modèle complet et les pièces que l'animation manipule. */
export interface DealerRig {
  root: THREE.Group;
  character: CharacterRig;
  /** Main droite du croupier (à gauche de l'écran) : c'est elle qui distribue. */
  dealingHand: HandRig;
  /** Main gauche du croupier (à droite de l'écran) : elle tient le paquet. */
  deckHand: HandRig;
  /** Carte tenue par la main qui distribue, entre la prise sur le paquet et le lancer. */
  heldCard: THREE.Group;
  /** Cartes lancées, réutilisées à tour de rôle ; exprimées dans le repère de `root`. */
  flyingCards: THREE.Group[];
}

/** Construit le croupier : toilettes, personnage, deux mains gantées, paquet et cartes. */
export function buildDealer(): DealerRig {
  const materials = createMaterials();
  const cards = new CardFactory(materials);
  const root = new THREE.Group();

  root.add(buildToilet(materials));

  const character = buildCharacter(materials);
  root.add(character.root);

  // Main qui distribue : paume vers le bas, pouce vers l'intérieur.
  const dealingHand = buildHand(materials, 1);
  const heldCard = cards.createCard();
  heldCard.position.set(0, 0.13, 0.045);
  heldCard.visible = false;
  dealingHand.root.add(heldCard);
  root.add(dealingHand.root);

  // Main qui tient le paquet : paume vers le haut, le paquet posé dessus.
  const deckHand = buildHand(materials, -1);
  const deckThickness = 0.05;
  const deck = cards.createDeck(deckThickness);
  deck.position.set(0, 0.075, 0.033 + deckThickness / 2 + 0.004);
  deckHand.root.add(deck);
  root.add(deckHand.root);

  const flyingCards = Array.from({ length: FLYING_CARDS }, () => {
    const card = cards.createCard();
    card.visible = false;
    root.add(card);
    return card;
  });

  return { root, character, dealingHand, deckHand, heldCard, flyingCards };
}

/** Libère la mémoire graphique de tout le modèle (géométries et matériaux). */
export function disposeDealer(rig: DealerRig): void {
  const materials = new Set<THREE.Material>();

  rig.root.traverse((object) => {
    const mesh = object as Partial<THREE.Mesh>;
    mesh.geometry?.dispose();
    for (const material of [mesh.material].flat()) {
      if (material) {
        materials.add(material);
      }
    }
  });

  materials.forEach((material) => material.dispose());
}
