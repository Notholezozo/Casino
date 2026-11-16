namespace Casino.Domain.Blackjack;

/// <summary>
/// Mise d'un joueur : un nombre de gorgées, ou un cul sec. Le cul sec est une unité à part
/// (on ne le convertit jamais en gorgées) ; doublé, il devient « 2 culs secs ».
/// </summary>
public readonly record struct Bet(int Amount, DrinkUnit Unit)
{
    /// <summary>Passer la manche.</summary>
    public static Bet Pass => new(0, DrinkUnit.Sip);

    public static Bet CulSec => new(1, DrinkUnit.CulSec);

    public static Bet Sips(int amount) => new(amount, DrinkUnit.Sip);

    public bool IsPass => Amount == 0;
}
