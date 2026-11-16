// Géométrie de la table en demi-cercle, dans le repère du dessin SVG (1000 × 600).
// Le dessin (table-page.html) et le placement des places partagent ces valeurs :
// modifier la forme se fait ici.

export const TABLE_VIEWBOX = { width: 1000, height: 600 };

/** Centre de l'arc : milieu du bord droit côté dealer. */
const ARC_CENTER = { x: 500, y: 130 };

/** Demi-axes de l'ellipse sur laquelle sont posées les places. */
const SEAT_RADIUS = { x: 372, y: 300 };

/** Demi-axes de l'ellipse des clous dorés, au milieu du rail. */
const STUD_RADIUS = { x: 473, y: 443 };

export interface TablePoint {
  /** Position en pourcentage de la largeur de la table. */
  x: number;
  /** Position en pourcentage de la hauteur de la table. */
  y: number;
}

function pointOnArc(radius: { x: number; y: number }, degrees: number): { x: number; y: number } {
  const angle = (degrees * Math.PI) / 180;
  return {
    x: ARC_CENTER.x + radius.x * Math.cos(angle),
    y: ARC_CENTER.y + radius.y * Math.sin(angle),
  };
}

/**
 * Centres des six places d'une table standard, en coordonnées du dessin, de la gauche vers
 * la droite de l'écran. Réglés à la main : une place fait environ 146 × 200 unités, et six
 * places de cette taille ne tiennent pas sur un arc régulier sans se chevaucher sur les côtés.
 */
const SIX_SEATS = [
  { x: 128, y: 152 },
  { x: 262, y: 332 },
  { x: 421, y: 430 },
  { x: 579, y: 430 },
  { x: 738, y: 332 },
  { x: 872, y: 152 },
];

/**
 * Centres des places, de la gauche vers la droite de l'écran.
 * Pour un autre nombre de places que six, elles sont réparties régulièrement sur l'arc
 * (angles mesurés depuis l'horizontale : 162° à gauche du dealer, 18° à sa droite).
 */
export function seatPositions(count: number): TablePoint[] {
  const first = 162;
  const last = 18;
  const points =
    count === SIX_SEATS.length
      ? SIX_SEATS
      : Array.from({ length: count }, (_, index) =>
          pointOnArc(
            SEAT_RADIUS,
            count === 1 ? 90 : first + ((last - first) * index) / (count - 1),
          ),
        );

  return points.map((point) => ({
    x: (point.x / TABLE_VIEWBOX.width) * 100,
    y: (point.y / TABLE_VIEWBOX.height) * 100,
  }));
}

/** Clous dorés du rail, en coordonnées du dessin. */
export const RAIL_STUDS = Array.from({ length: 13 }, (_, index) =>
  pointOnArc(STUD_RADIUS, 6 + index * 14),
);
