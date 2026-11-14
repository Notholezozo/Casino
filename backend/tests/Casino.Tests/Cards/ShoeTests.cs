using Casino.Domain.Blackjack;
using Casino.Domain.Cards;

namespace Casino.Tests.Cards;

public class ShoeTests
{
    [Fact]
    public void Four_decks_hold_208_cards_with_four_copies_of_each()
    {
        var shoe = new Shoe(4, new CryptoRandomSource());

        var cards = Enumerable.Range(0, 208).Select(_ => shoe.Draw()).ToList();

        Assert.Equal(208, shoe.Total);
        Assert.Equal(0, shoe.Remaining);
        Assert.Equal(52, cards.Distinct().Count());
        Assert.All(cards.GroupBy(c => c), g => Assert.Equal(4, g.Count()));
    }

    [Fact]
    public void Cards_are_only_reshuffled_once_the_shoe_is_empty()
    {
        var shoe = new Shoe(1, new CryptoRandomSource());
        var firstPass = Enumerable.Range(0, 50).Select(_ => shoe.Draw()).ToList();
        shoe.Discard(firstPass);

        // Les deux dernières cartes sortent avant tout remélange.
        var last = new[] { shoe.Draw(), shoe.Draw() };
        Assert.Empty(last.Intersect(firstPass));
        Assert.Equal(0, shoe.Remaining);

        // Sabot vide : la défausse est remélangée, sans les cartes encore sur la table.
        var next = shoe.Draw();
        Assert.Contains(next, firstPass);
        Assert.Equal(49, shoe.Remaining);
    }

    [Fact]
    public void Drawing_with_nothing_left_anywhere_fails()
    {
        var shoe = new Shoe(1, new CryptoRandomSource());
        for (var i = 0; i < 52; i++)
        {
            shoe.Draw();
        }

        Assert.Throws<InvalidOperationException>(() => shoe.Draw());
    }
}

public class HandEvaluatorTests
{
    [Theory]
    [InlineData(new[] { Rank.Ace, Rank.King }, 21, true)]
    [InlineData(new[] { Rank.Ace, Rank.Six }, 17, true)]
    [InlineData(new[] { Rank.Ace, Rank.Six, Rank.Ten }, 17, false)]
    [InlineData(new[] { Rank.Ace, Rank.Ace }, 12, true)]
    [InlineData(new[] { Rank.Ace, Rank.Ace, Rank.Nine }, 21, true)]
    [InlineData(new[] { Rank.Ten, Rank.Queen, Rank.Two }, 22, false)]
    [InlineData(new[] { Rank.Five, Rank.Jack }, 15, false)]
    public void Evaluates_totals_and_softness(Rank[] ranks, int total, bool isSoft)
    {
        var value = HandEvaluator.Evaluate(ranks.Select(r => new Card(r, Suit.Hearts)));

        Assert.Equal(new HandValue(total, isSoft), value);
    }

    [Fact]
    public void Only_two_card_twenty_one_is_a_natural()
    {
        Card[] natural = [new(Rank.Ace, Suit.Clubs), new(Rank.Queen, Suit.Clubs)];
        Card[] threeCards = [new(Rank.Seven, Suit.Clubs), new(Rank.Seven, Suit.Hearts), new(Rank.Seven, Suit.Spades)];

        Assert.True(HandEvaluator.IsNatural(natural));
        Assert.False(HandEvaluator.IsNatural(threeCards));
    }
}
