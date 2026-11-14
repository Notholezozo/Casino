using Casino.Domain.Blackjack;

namespace Casino.Application.Blackjack.Contracts;

// ---------- Commandes ----------

/// <summary>Mise en gorgées. 0 = passer la manche.</summary>
public sealed record PlaceBetRequest(int Sips);

public sealed record PlayerActionRequest(PlayerAction Action);

public sealed record SipAllocationDto(Guid PlayerId, int Sips);

/// <summary>Répartition des gorgées gagnées. La somme doit être égale au total à distribuer.</summary>
public sealed record DistributeRequest(IReadOnlyList<SipAllocationDto> Allocations);

// ---------- Vues ----------

/// <param name="Rank">"A", "2" … "10", "J", "Q", "K".</param>
/// <param name="Suit">"Clubs", "Diamonds", "Hearts" ou "Spades".</param>
public sealed record CardDto(string Rank, string Suit);

public sealed record HandDto(
    IReadOnlyList<CardDto> Cards,
    int Total,
    bool IsSoft,
    int Bet,
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

public sealed record SipTransferDto(
    Guid FromPlayerId,
    string FromPlayerName,
    Guid ToPlayerId,
    string ToPlayerName,
    int Sips);

/// <summary>Bilan d'un joueur pour la manche, disponible de la fin du tour du croupier jusqu'à la donne suivante.</summary>
/// <param name="SipsLost">Gorgées perdues sur ses propres mains.</param>
/// <param name="SipsToDistribute">Gorgées gagnées, à distribuer aux autres.</param>
/// <param name="CanDistribute">Faux s'il n'y a personne à qui distribuer.</param>
/// <param name="SipsToDrink">Total à boire : perdues + reçues des autres joueurs.</param>
public sealed record RoundResultDto(
    int SipsLost,
    int SipsToDistribute,
    bool CanDistribute,
    bool DistributionDone,
    IReadOnlyList<SipTransferDto> Received,
    IReadOnlyList<SipTransferDto> Given,
    int SipsToDrink);

public sealed record BetLimitsDto(int Min, int Max);

public sealed record ShoeDto(int Remaining, int Total);

/// <summary>Autre joueur de la table, tel que vu depuis un téléphone (jamais ses cartes).</summary>
public sealed record OtherPlayerDto(Guid Id, string Name, bool IsConnected, bool InRound);

public sealed record PlayerSelfDto(
    Guid Id,
    string Name,
    int? Bet,
    bool InRound,
    IReadOnlyList<HandDto> Hands,
    bool IsMyTurn,
    IReadOnlyList<PlayerAction> AllowedActions,
    RoundResultDto? Result,
    int TotalDrunk,
    int TotalGiven);

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
    bool InRound,
    IReadOnlyList<HandDto> Hands,
    bool IsActive,
    RoundResultDto? Result,
    int TotalDrunk,
    int TotalGiven);

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
    IReadOnlyList<SipTransferDto> Transfers);
