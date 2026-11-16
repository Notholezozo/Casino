using Casino.Domain.Cards;

namespace Casino.Domain.Poker;

public enum PokerHandCategory
{
    HighCard,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
}

/// <summary>Valeur d'une main de poker. Deux mains se comparent par <see cref="Score"/> : le plus grand gagne.</summary>
public readonly record struct PokerHandValue(PokerHandCategory Category, long Score) : IComparable<PokerHandValue>
{
    /// <summary>Nom affiché, dans le vocabulaire anglais du jeu.</summary>
    public string Name => Category switch
    {
        PokerHandCategory.Pair => "Pair",
        PokerHandCategory.TwoPair => "Two pair",
        PokerHandCategory.ThreeOfAKind => "Three of a kind",
        PokerHandCategory.Straight => "Straight",
        PokerHandCategory.Flush => "Flush",
        PokerHandCategory.FullHouse => "Full house",
        PokerHandCategory.FourOfAKind => "Four of a kind",
        PokerHandCategory.StraightFlush => "Straight flush",
        _ => "High card",
    };

    public int CompareTo(PokerHandValue other) => Score.CompareTo(other.Score);
}

public static class PokerHandEvaluator
{
    private const int HandSize = 5;

    /// <summary>Meilleure main de cinq cartes parmi les cartes fournies (cinq à sept).</summary>
    public static PokerHandValue Evaluate(IReadOnlyList<Card> cards)
    {
        if (cards.Count < HandSize)
        {
            throw new ArgumentException("Il faut au moins cinq cartes.", nameof(cards));
        }

        return Combinations(cards, HandSize).Select(EvaluateFive).Max();
    }

    private static PokerHandValue EvaluateFive(IReadOnlyList<Card> hand)
    {
        var ranks = hand.Select(c => (int)c.Rank).OrderDescending().ToList();
        var isFlush = hand.All(c => c.Suit == hand[0].Suit);
        var straightHigh = StraightHigh(ranks);

        // Rangs regroupés : d'abord les plus nombreux (carré, brelan, paires), puis les plus hauts.
        var groups = ranks.GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key)
            .ToList();
        var counts = groups.Select(g => g.Count()).ToList();
        var byGroup = groups.Select(g => g.Key).ToList();

        return (isFlush, straightHigh, counts) switch
        {
            (true, > 0, _) => Value(PokerHandCategory.StraightFlush, [straightHigh]),
            (_, _, [4, ..]) => Value(PokerHandCategory.FourOfAKind, byGroup),
            (_, _, [3, 2]) => Value(PokerHandCategory.FullHouse, byGroup),
            (true, _, _) => Value(PokerHandCategory.Flush, ranks),
            (_, > 0, _) => Value(PokerHandCategory.Straight, [straightHigh]),
            (_, _, [3, ..]) => Value(PokerHandCategory.ThreeOfAKind, byGroup),
            (_, _, [2, 2, ..]) => Value(PokerHandCategory.TwoPair, byGroup),
            (_, _, [2, ..]) => Value(PokerHandCategory.Pair, byGroup),
            _ => Value(PokerHandCategory.HighCard, ranks),
        };
    }

    /// <summary>Carte haute de la suite, ou 0. La « roue » A-2-3-4-5 est une suite au cinq.</summary>
    private static int StraightHigh(List<int> descendingRanks)
    {
        if (descendingRanks.Distinct().Count() != HandSize)
        {
            return 0;
        }

        if (descendingRanks[0] - descendingRanks[4] == 4)
        {
            return descendingRanks[0];
        }

        return descendingRanks is [(int)Rank.Ace, 5, 4, 3, 2] ? 5 : 0;
    }

    /// <summary>La catégorie d'abord, puis les rangs qui départagent, du plus au moins important.</summary>
    private static PokerHandValue Value(PokerHandCategory category, IReadOnlyList<int> tiebreakers)
    {
        long score = (int)category;
        for (var i = 0; i < HandSize; i++)
        {
            score = score * 15 + (i < tiebreakers.Count ? tiebreakers[i] : 0);
        }

        return new PokerHandValue(category, score);
    }

    private static IEnumerable<IReadOnlyList<Card>> Combinations(IReadOnlyList<Card> cards, int size)
    {
        var indexes = Enumerable.Range(0, size).ToArray();
        while (true)
        {
            yield return indexes.Select(i => cards[i]).ToList();

            var position = size - 1;
            while (position >= 0 && indexes[position] == cards.Count - size + position)
            {
                position--;
            }

            if (position < 0)
            {
                yield break;
            }

            indexes[position]++;
            for (var i = position + 1; i < size; i++)
            {
                indexes[i] = indexes[i - 1] + 1;
            }
        }
    }
}
