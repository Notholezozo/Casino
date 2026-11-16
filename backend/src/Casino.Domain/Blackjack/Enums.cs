namespace Casino.Domain.Blackjack;

/// <summary>Étapes d'une manche, dans l'ordre où elles s'enchaînent.</summary>
public enum GamePhase
{
    /// <summary>Les joueurs choisissent leur mise (ou passent).</summary>
    Betting,

    /// <summary>Chaque main est jouée à tour de rôle.</summary>
    PlayerTurns,

    /// <summary>Le croupier retourne sa carte puis tire, une carte par pas d'horloge.</summary>
    DealerTurn,

    /// <summary>Les gagnants distribuent leurs gorgées.</summary>
    Distribution,

    /// <summary>Affichage du bilan avant la manche suivante.</summary>
    Results,
}

public enum HandStatus
{
    Playing,
    Stood,
    Doubled,
    Bust,
    Blackjack,
}

public enum HandOutcome
{
    Win,
    Blackjack,
    Push,
    Lose,
}

/// <summary>Unité d'une mise : des gorgées, ou un cul sec (finir son verre), compté à part.</summary>
public enum DrinkUnit
{
    Sip,
    CulSec,
}

public enum PlayerAction
{
    Hit,
    Stand,
    Double,
    Split,
}
