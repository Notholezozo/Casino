using Casino.Application.Blackjack.Contracts;
using Casino.Domain.Poker;

namespace Casino.Application.Poker.Contracts;

// Les cartes, les transferts de gorgées et la répartition (DistributeRequest) sont communs
// aux jeux et viennent des contrats du blackjack.

// ---------- Commandes ----------

/// <param name="RaiseBy">Pour une relance : gorgées ajoutées à l'enchère en cours. Ignoré sinon.</param>
public sealed record PokerActionRequest(PokerAction Action, int RaiseBy = 0);

// ---------- Vues ----------

/// <summary>Une place de la table de poker.</summary>
/// <param name="HasCards">Vrai si le joueur a des cartes en main, visibles ou non.</param>
/// <param name="Cards">
/// Ses deux cartes, ou null si celui qui regarde n'a pas le droit de les voir : seul le joueur
/// voit les siennes, et tout le monde voit celles des joueurs encore en course à l'abattage.
/// </param>
/// <param name="StreetBet">Gorgées misées pendant le tour d'enchères en cours.</param>
/// <param name="TotalBet">Gorgées misées depuis le début de la main.</param>
/// <param name="HandName">Nom de sa main ("Pair", "Flush"…), connu à l'abattage.</param>
/// <param name="ToDistribute">Part du pot à distribuer, pour un gagnant.</param>
/// <param name="SipsToDrink">Gorgées reçues des gagnants pendant cette main.</param>
public sealed record PokerSeatDto(
    Guid PlayerId,
    string Name,
    bool IsConnected,
    bool InHand,
    bool Folded,
    bool HasCards,
    IReadOnlyList<CardDto>? Cards,
    int StreetBet,
    int TotalBet,
    bool IsActive,
    bool IsButton,
    bool IsSmallBlind,
    bool IsBigBlind,
    bool IsWinner,
    string? HandName,
    int ToDistribute,
    bool DistributionDone,
    int SipsToDrink,
    DrinkTotalsDto Totals);

public sealed record PokerLimitsDto(int SmallBlind, int BigBlind, int MinRaise, int MaxRaise);

/// <summary>Ce qui ne concerne que le joueur qui regarde : ses options du moment.</summary>
/// <param name="AmountToCall">Gorgées à ajouter pour suivre (0 s'il peut checker).</param>
/// <param name="CanDistribute">Faux s'il n'y a personne à qui distribuer.</param>
public sealed record PokerSelfDto(
    Guid Id,
    bool IsMyTurn,
    IReadOnlyList<PokerAction> AllowedActions,
    int AmountToCall,
    bool CanDistribute);

/// <summary>
/// État envoyé à l'écran table. Il est visible de tous : les cartes privées n'y figurent
/// qu'à l'abattage.
/// </summary>
public record PokerTableViewDto
{
    /// <summary>Compteur croissant ; un client ignore tout état plus ancien que le dernier reçu.</summary>
    public required long Version { get; init; }

    public required string TableCode { get; init; }

    public required PokerPhase Phase { get; init; }

    /// <summary>Temps restant avant l'échéance de la phase, ou null s'il n'y a pas de minuteur.</summary>
    public required int? RemainingMs { get; init; }

    public required int HandNumber { get; init; }

    /// <summary>Cartes communes déjà posées (0, 3, 4 ou 5).</summary>
    public required IReadOnlyList<CardDto> Board { get; init; }

    public required int Pot { get; init; }

    /// <summary>Enchère à égaler pendant le tour en cours.</summary>
    public required int CurrentBet { get; init; }

    public required IReadOnlyList<PokerSeatDto> Seats { get; init; }

    public required Guid? ActivePlayerId { get; init; }

    public required int MaxPlayers { get; init; }

    public required PokerLimitsDto Limits { get; init; }

    /// <summary>Vrai si la main est allée à l'abattage.</summary>
    public required bool Showdown { get; init; }

    /// <summary>Vrai du règlement d'une main jusqu'à la donne suivante.</summary>
    public required bool ResultsAvailable { get; init; }

    public required IReadOnlyList<DrinkTransferDto> Transfers { get; init; }
}

/// <summary>État envoyé à un joueur : la table, ses propres cartes et ses options.</summary>
public sealed record PokerPlayerViewDto : PokerTableViewDto
{
    public required PokerSelfDto Me { get; init; }
}
