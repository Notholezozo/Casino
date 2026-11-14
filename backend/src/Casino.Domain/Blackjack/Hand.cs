using Casino.Domain.Cards;

namespace Casino.Domain.Blackjack;

/// <summary>Valeur d'une main : meilleur total, et « soft » si un as y compte encore pour 11.</summary>
public readonly record struct HandValue(int Total, bool IsSoft)
{
    public bool IsBust => Total > 21;
}

public static class HandEvaluator
{
    public static int CardValue(Card card) => card.Rank switch
    {
        Rank.Ace => 11,
        Rank.Jack or Rank.Queen or Rank.King => 10,
        _ => (int)card.Rank,
    };

    public static HandValue Evaluate(IEnumerable<Card> cards)
    {
        var total = 0;
        var softAces = 0;

        foreach (var card in cards)
        {
            total += CardValue(card);
            if (card.Rank == Rank.Ace)
            {
                softAces++;
            }
        }

        while (total > 21 && softAces > 0)
        {
            total -= 10;
            softAces--;
        }

        return new HandValue(total, softAces > 0);
    }

    /// <summary>Blackjack naturel : 21 avec exactement deux cartes.</summary>
    public static bool IsNatural(IReadOnlyCollection<Card> cards) =>
        cards.Count == 2 && Evaluate(cards).Total == 21;
}

/// <summary>Une main de joueur et sa mise. Un joueur en a plusieurs après une séparation.</summary>
public sealed class Hand
{
    private readonly List<Card> _cards = [];

    internal Hand(int bet, bool isFromSplit = false)
    {
        Bet = bet;
        IsFromSplit = isFromSplit;
    }

    public IReadOnlyList<Card> Cards => _cards;

    public int Bet { get; private set; }

    public HandStatus Status { get; private set; } = HandStatus.Playing;

    public HandOutcome? Outcome { get; private set; }

    /// <summary>Une main issue d'une séparation ne peut pas faire de blackjack naturel.</summary>
    public bool IsFromSplit { get; }

    public HandValue Value => HandEvaluator.Evaluate(_cards);

    public bool CanDouble => Status == HandStatus.Playing && _cards.Count == 2;

    public bool IsPair =>
        _cards.Count == 2 && HandEvaluator.CardValue(_cards[0]) == HandEvaluator.CardValue(_cards[1]);

    /// <summary>Vrai si la main attend d'être comparée au croupier (ni sautée, ni blackjack).</summary>
    public bool IsStanding => Status is HandStatus.Stood or HandStatus.Doubled;

    internal void Add(Card card) => _cards.Add(card);

    internal void MarkBlackjack() => Status = HandStatus.Blackjack;

    internal void Stand() => Status = HandStatus.Stood;

    internal void MarkBust() => Status = HandStatus.Bust;

    internal void Double()
    {
        Bet *= 2;
        Status = HandStatus.Doubled;
    }

    internal void Settle(HandOutcome outcome) => Outcome = outcome;
}
