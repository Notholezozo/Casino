using Casino.Domain.Blackjack;

namespace Casino.Domain.Entities;

/// <summary>Manche de blackjack terminée (historique).</summary>
public sealed class Round
{
    public Guid Id { get; set; }

    public Guid TableId { get; set; }

    public int Number { get; set; }

    public DateTime EndedAtUtc { get; set; }

    /// <summary>Cartes du croupier en notation compacte, ex. "10S 6H KD".</summary>
    public required string DealerCards { get; set; }

    public int DealerTotal { get; set; }

    public List<RoundHand> Hands { get; set; } = [];

    public List<RoundDrinkTransfer> Transfers { get; set; } = [];
}

public sealed class RoundHand
{
    public Guid Id { get; set; }

    public Guid RoundId { get; set; }

    public Guid PlayerId { get; set; }

    public required string Cards { get; set; }

    public int Total { get; set; }

    /// <summary>Mise finale (doublée le cas échéant), dans l'unité <see cref="Unit"/>.</summary>
    public int Bet { get; set; }

    public DrinkUnit Unit { get; set; }

    public HandOutcome Outcome { get; set; }
}

/// <summary>Ce qu'un gagnant a fait boire à un autre joueur : des gorgées ou des culs secs.</summary>
public sealed class RoundDrinkTransfer
{
    public Guid Id { get; set; }

    public Guid RoundId { get; set; }

    public Guid FromPlayerId { get; set; }

    public Guid ToPlayerId { get; set; }

    public int Amount { get; set; }

    public DrinkUnit Unit { get; set; }
}
