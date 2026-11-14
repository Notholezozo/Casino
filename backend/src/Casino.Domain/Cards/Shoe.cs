using System.Security.Cryptography;

namespace Casino.Domain.Cards;

/// <summary>Source d'aléa injectable, pour rendre le moteur de jeu déterministe en test.</summary>
public interface IRandomSource
{
    /// <summary>Entier uniforme dans [0, exclusiveMax).</summary>
    int Next(int exclusiveMax);
}

public sealed class CryptoRandomSource : IRandomSource
{
    public int Next(int exclusiveMax) => RandomNumberGenerator.GetInt32(exclusiveMax);
}

public interface IShoe
{
    int Remaining { get; }

    int Total { get; }

    Card Draw();

    /// <summary>Remet des cartes jouées à la défausse ; elles ne reviennent en jeu qu'au prochain mélange.</summary>
    void Discard(IEnumerable<Card> cards);
}

/// <summary>
/// Sabot de plusieurs jeux. Les cartes ne sont remélangées que lorsque le sabot est vide :
/// la défausse est alors mélangée et redevient le sabot (les cartes encore sur la table n'en font pas partie).
/// </summary>
public sealed class Shoe : IShoe
{
    private readonly IRandomSource _random;
    private readonly List<Card> _cards;
    private readonly List<Card> _discard = [];

    public Shoe(int deckCount, IRandomSource random)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(deckCount, 1);
        _random = random;
        _cards = Enumerable.Range(0, deckCount).SelectMany(_ => Card.StandardDeck()).ToList();
        Total = _cards.Count;
        Shuffle();
    }

    public int Remaining => _cards.Count;

    public int Total { get; }

    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            if (_discard.Count == 0)
            {
                throw new InvalidOperationException("Le sabot et la défausse sont vides.");
            }

            _cards.AddRange(_discard);
            _discard.Clear();
            Shuffle();
        }

        var card = _cards[^1];
        _cards.RemoveAt(_cards.Count - 1);
        return card;
    }

    public void Discard(IEnumerable<Card> cards) => _discard.AddRange(cards);

    /// <summary>Mélange de Fisher-Yates.</summary>
    private void Shuffle()
    {
        for (var i = _cards.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }
}
