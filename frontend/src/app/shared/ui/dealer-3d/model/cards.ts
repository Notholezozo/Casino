import * as THREE from 'three';
import { DealerMaterials, solid } from './materials';

// Format d'une carte dans le repère d'une main : longueur le long des doigts (Y), épaisseur en Z.
const CARD_WIDTH = 0.12;
const CARD_LENGTH = 0.17;
const CARD_THICKNESS = 0.005;
/** Largeur du liseré ivoire autour du dos bordeaux. */
const BORDER = 0.009;

/** Géométries communes à toutes les cartes : créées une fois, partagées. */
export class CardFactory {
  private readonly body = new THREE.BoxGeometry(CARD_WIDTH, CARD_LENGTH, CARD_THICKNESS);
  private readonly back = new THREE.PlaneGeometry(CARD_WIDTH - 2 * BORDER, CARD_LENGTH - 2 * BORDER);
  private readonly diamond = new THREE.PlaneGeometry(0.03, 0.03);

  constructor(private readonly materials: DealerMaterials) {}

  /** Une carte face cachée : dos bordeaux à liseré ivoire des deux côtés. */
  createCard(): THREE.Group {
    const card = new THREE.Group();
    card.add(solid(this.body, this.materials.glove));
    card.add(this.createBack(CARD_THICKNESS / 2 + 0.0006, false));
    card.add(this.createBack(-(CARD_THICKNESS / 2 + 0.0006), true));
    return card;
  }

  /** Le paquet tenu en main : une pile dont on voit les tranches ivoire et le dos de la première carte. */
  createDeck(thickness: number): THREE.Group {
    const deck = new THREE.Group();
    const pile = solid(new THREE.BoxGeometry(CARD_WIDTH, CARD_LENGTH, thickness), this.materials.glove);
    deck.add(pile);

    const top = this.createBack(thickness / 2 + 0.0008, false);
    const mark = new THREE.Mesh(this.diamond, this.materials.gold);
    mark.rotation.z = Math.PI / 4;
    mark.position.z = 0.0008;
    top.add(mark);
    deck.add(top);

    return deck;
  }

  private createBack(z: number, flipped: boolean): THREE.Mesh {
    const back = new THREE.Mesh(this.back, this.materials.burgundy);
    back.position.z = z;
    back.rotation.y = flipped ? Math.PI : 0;
    back.receiveShadow = true;
    return back;
  }
}
