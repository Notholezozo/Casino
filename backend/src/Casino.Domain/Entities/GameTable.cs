namespace Casino.Domain.Entities;

public enum GameType
{
    Blackjack,
    Poker,
}

/// <summary>Table de jeu (room) telle qu'elle est enregistrée en base.</summary>
public sealed class GameTable
{
    public Guid Id { get; set; }

    /// <summary>Code court que les joueurs saisissent pour rejoindre.</summary>
    public required string Code { get; set; }

    public GameType Game { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Null tant que la table est ouverte.</summary>
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>Mise minimale en gorgées, choisie à la création.</summary>
    public int MinBet { get; set; }

    /// <summary>Mise maximale en gorgées, choisie à la création.</summary>
    public int MaxBet { get; set; }

    public bool AllowCulSec { get; set; }

    public List<Player> Players { get; set; } = [];

    public List<Round> Rounds { get; set; } = [];
}

/// <summary>Joueur invité, rattaché à une seule table.</summary>
public sealed class Player
{
    public Guid Id { get; set; }

    public Guid TableId { get; set; }

    public required string Name { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public DateTime? LeftAtUtc { get; set; }

    /// <summary>Gorgées bues depuis l'arrivée à la table.</summary>
    public int TotalDrunk { get; set; }

    /// <summary>Gorgées distribuées depuis l'arrivée à la table.</summary>
    public int TotalGiven { get; set; }

    public int TotalCulsSecsDrunk { get; set; }

    public int TotalCulsSecsGiven { get; set; }
}
