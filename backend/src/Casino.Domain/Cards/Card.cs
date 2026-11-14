namespace Casino.Domain.Cards;

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades,
}

public enum Rank
{
    Two = 2,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace,
}

/// <summary>Carte à jouer, indépendante du jeu (blackjack, poker…).</summary>
public readonly record struct Card(Rank Rank, Suit Suit)
{
    /// <summary>"A", "2" … "10", "J", "Q", "K".</summary>
    public string RankLabel => Rank switch
    {
        Rank.Ace => "A",
        Rank.King => "K",
        Rank.Queen => "Q",
        Rank.Jack => "J",
        _ => ((int)Rank).ToString(),
    };

    /// <summary>Notation compacte pour l'historique, ex. "AS" (as de pique) ou "10H".</summary>
    public override string ToString() => RankLabel + Suit.ToString()[0];

    /// <summary>Un jeu complet de 52 cartes, dans un ordre fixe.</summary>
    public static IEnumerable<Card> StandardDeck()
    {
        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                yield return new Card(rank, suit);
            }
        }
    }
}
