using Casino.Domain.Blackjack;

namespace Casino.Application.Blackjack.Contracts;

// ---------- Commandes ----------

/// <summary>Mise : un nombre de gorgées (0 = passer la manche), ou un cul sec si la table l'autorise.</summary>
public sealed record PlaceBetRequest(int Sips, bool CulSec = false);

public sealed record PlayerActionRequest(PlayerAction Action);

/// <param name="Amount">Quantité donnée, dans l'unité de la mise du gagnant (gorgées ou culs secs).</param>
public sealed record DrinkAllocationDto(Guid PlayerId, int Amount);

/// <summary>Répartition de ce qui a été gagné. La somme doit être égale au total à distribuer.</summary>
public sealed record DistributeRequest(IReadOnlyList<DrinkAllocationDto> Allocations);

// ---------- Vues ----------

/// <param name="Rank">"A", "2" … "10", "J", "Q", "K".</param>
/// <param name="Suit">"Clubs", "Diamonds", "Hearts" ou "Spades".</param>
public sealed record CardDto(string Rank, string Suit);

/// <param name="Bet">Quantité misée sur cette main, dans l'unité <paramref name="Unit"/>.</param>
public sealed record HandDto(
    IReadOnlyList<CardDto> Cards,
    int Total,
    bool IsSoft,
    int Bet,
    DrinkUnit Unit,
    HandStatus Status,
    HandOutcome? Outcome,
    bool IsActive);

/// <summary>Main du croupier. Tant que <see cref="HasHiddenCard"/> est vrai, seule la carte visible est envoyée.</summary>
public sealed record DealerDto(
    IReadOnlyList<CardDto> Cards,
    bool HasHiddenCard,
    int Total,
    bool IsSoft,
    bool IsBlackjack,
    bool IsBust);

public sealed record DrinkTransferDto(
    Guid FromPlayerId,
    string FromPlayerName,
    Guid ToPlayerId,
    string ToPlayerName,
    int Amount,
    DrinkUnit Unit);

/// <summary>Bilan d'un joueur pour la manche, disponible de la fin du tour du croupier jusqu'à la donne suivante.</summary>
/// <param name="Unit">Unité de <paramref name="Lost"/> et <paramref name="ToDistribute"/> : celle de sa mise.</param>
/// <param name="Lost">Quantité perdue sur ses propres mains.</param>
/// <param name="ToDistribute">Quantité gagnée, à distribuer aux autres.</param>
/// <param name="CanDistribute">Faux s'il n'y a personne à qui distribuer.</param>
/// <param name="SipsToDrink">Gorgées à boire : perdues + reçues des autres joueurs.</param>
/// <param name="CulsSecsToDrink">Culs secs à boire : perdus + reçus des autres joueurs.</param>
public sealed record RoundResultDto(
    DrinkUnit Unit,
    int Lost,
    int ToDistribute,
    bool CanDistribute,
    bool DistributionDone,
    IReadOnlyList<DrinkTransferDto> Received,
    IReadOnlyList<DrinkTransferDto> Given,
    int SipsToDrink,
    int CulsSecsToDrink);

/// <summary>Mises acceptées par la table, choisies à sa création.</summary>
public sealed record BetLimitsDto(int Min, int Max, bool AllowCulSec);

public sealed record ShoeDto(int Remaining, int Total);

/// <summary>Cumuls d'un joueur depuis son arrivée à la table.</summary>
public sealed record DrinkTotalsDto(int SipsDrunk, int SipsGiven, int CulsSecsDrunk, int CulsSecsGiven);

/// <summary>Autre joueur de la table, tel que vu depuis un téléphone (jamais ses cartes).</summary>
public sealed record OtherPlayerDto(Guid Id, string Name, bool IsConnected, bool InRound);

/// <param name="Bet">Quantité misée : null = pas encore répondu, 0 = passe.</param>
public sealed record PlayerSelfDto(
    Guid Id,
    string Name,
    int? Bet,
    DrinkUnit BetUnit,
    bool InRound,
    IReadOnlyList<HandDto> Hands,
    bool IsMyTurn,
    IReadOnlyList<PlayerAction> AllowedActions,
    RoundResultDto? Result,
    DrinkTotalsDto Totals);

/// <summary>État envoyé à un joueur : ses cartes, le croupier et ses options.</summary>
/// <param name="Version">Compteur croissant ; un client ignore tout état plus ancien que le dernier reçu.</param>
/// <param name="RemainingMs">Temps restant avant l'échéance de la phase, ou null s'il n'y a pas de minuteur.</param>
public sealed record PlayerViewDto(
    long Version,
    string TableCode,
    GamePhase Phase,
    int? RemainingMs,
    int RoundNumber,
    DealerDto Dealer,
    PlayerSelfDto Me,
    IReadOnlyList<OtherPlayerDto> Others,
    Guid? ActivePlayerId,
    BetLimitsDto BetLimits);

public sealed record SeatDto(
    Guid PlayerId,
    string Name,
    bool IsConnected,
    int? Bet,
    DrinkUnit BetUnit,
    bool InRound,
    IReadOnlyList<HandDto> Hands,
    bool IsActive,
    RoundResultDto? Result,
    DrinkTotalsDto Totals);

/// <summary>État envoyé à la page table (admin) : tous les sièges et toutes les cartes.</summary>
public sealed record TableViewDto(
    long Version,
    string TableCode,
    GamePhase Phase,
    int? RemainingMs,
    int RoundNumber,
    DealerDto Dealer,
    IReadOnlyList<SeatDto> Seats,
    Guid? ActivePlayerId,
    int MaxPlayers,
    ShoeDto Shoe,
    BetLimitsDto BetLimits,
    IReadOnlyList<DrinkTransferDto> Transfers);
