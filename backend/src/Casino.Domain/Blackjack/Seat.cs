namespace Casino.Domain.Blackjack;

/// <summary>Un joueur assis à la table et son état pour la manche en cours.</summary>
public sealed class Seat
{
    private readonly List<Hand> _hands = [];

    internal Seat(Guid playerId, string name)
    {
        PlayerId = playerId;
        Name = name;
    }

    public Guid PlayerId { get; }

    public string Name { get; }

    public bool IsConnected { get; internal set; }

    /// <summary>Null = n'a pas encore répondu ; 0 = passe la manche.</summary>
    public int? Bet { get; internal set; }

    /// <summary>Vrai si le joueur a des cartes dans la manche en cours.</summary>
    public bool InRound { get; internal set; }

    public IReadOnlyList<Hand> Hands => _hands;

    /// <summary>Gorgées perdues sur ses propres mains pendant la manche.</summary>
    public int SipsLost { get; internal set; }

    /// <summary>Gorgées gagnées pendant la manche, à distribuer aux autres.</summary>
    public int SipsToDistribute { get; internal set; }

    public bool DistributionDone { get; internal set; }

    /// <summary>Cumul des gorgées bues depuis l'arrivée à la table.</summary>
    public int TotalDrunk { get; internal set; }

    /// <summary>Cumul des gorgées distribuées depuis l'arrivée à la table.</summary>
    public int TotalGiven { get; internal set; }

    internal List<Hand> MutableHands => _hands;

    internal void ResetRoundOutcome()
    {
        SipsLost = 0;
        SipsToDistribute = 0;
        DistributionDone = false;
    }
}

/// <summary>Gorgées données par un gagnant à un autre joueur. Les noms sont figés au moment du don.</summary>
public sealed record SipTransfer(Guid FromPlayerId, string FromPlayerName, Guid ToPlayerId, string ToPlayerName, int Sips);

/// <summary>Photo d'une manche terminée, destinée à l'historique.</summary>
public sealed record RoundSummary(
    int RoundNumber,
    IReadOnlyList<Cards.Card> DealerCards,
    int DealerTotal,
    IReadOnlyList<RoundHandSummary> Hands,
    IReadOnlyList<SipTransfer> Transfers,
    IReadOnlyList<PlayerTotals> Totals);

public sealed record RoundHandSummary(
    Guid PlayerId,
    IReadOnlyList<Cards.Card> Cards,
    int Total,
    int Bet,
    HandOutcome Outcome);

public sealed record PlayerTotals(Guid PlayerId, int TotalDrunk, int TotalGiven);
