namespace Casino.Domain.Poker;

/// <summary>Étapes d'une main de Texas Hold'em.</summary>
public enum PokerPhase
{
    /// <summary>Entre deux mains : on attend au moins deux joueurs connectés, puis on distribue.</summary>
    Waiting,
    PreFlop,
    Flop,
    Turn,
    River,

    /// <summary>Le ou les gagnants distribuent le pot en gorgées.</summary>
    Distribution,

    /// <summary>Affichage du résultat avant la main suivante.</summary>
    Results,
}

public enum PokerAction
{
    Fold,
    Check,
    Call,
    Raise,
}

/// <summary>
/// Règles d'une table de poker à gorgées. Il n'y a pas de tapis de jetons : les enchères sont
/// donc limitées (taille et nombre de relances), ce qui borne le pot et supprime les pots annexes.
/// </summary>
public sealed record PokerRules
{
    public int MaxPlayers { get; init; } = 6;

    public int SmallBlind { get; init; } = 1;

    public int BigBlind { get; init; } = 2;

    /// <summary>Plus petite relance autorisée, en gorgées ajoutées à l'enchère en cours.</summary>
    public int MinRaise { get; init; } = 1;

    /// <summary>Plus grosse relance autorisée, en gorgées ajoutées à l'enchère en cours.</summary>
    public int MaxRaise { get; init; } = 10;

    /// <summary>Nombre de mises/relances par tour d'enchères (les blindes ne comptent pas).</summary>
    public int MaxRaisesPerStreet { get; init; } = 3;
}

/// <summary>Durées des phases automatiques.</summary>
public sealed record PokerTimings
{
    /// <summary>Pause avant de distribuer une nouvelle main.</summary>
    public TimeSpan StartDelay { get; init; } = TimeSpan.FromSeconds(4);

    /// <summary>Temps par décision ; à l'échéance le joueur checke s'il le peut, sinon il se couche.</summary>
    public TimeSpan Turn { get; init; } = TimeSpan.FromSeconds(25);

    /// <summary>Temps pour distribuer le pot ; à l'échéance la répartition est automatique.</summary>
    public TimeSpan Distribution { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Durée d'affichage du résultat avant la main suivante.</summary>
    public TimeSpan Results { get; init; } = TimeSpan.FromSeconds(10);
}
