using Casino.Domain.Blackjack;

namespace Casino.Application.Common;

/// <summary>Réglages des tables (section "Tables" de la configuration).</summary>
public sealed class TablesOptions
{
    public const string Section = "Tables";

    public int MaxOpenTables { get; set; } = 3;

    /// <summary>Une table sans aucun écran connecté pendant cette durée est fermée automatiquement.</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>Règles et durées du blackjack (section "Blackjack" de la configuration).</summary>
public sealed class BlackjackOptions
{
    public const string Section = "Blackjack";

    public BlackjackRules Rules { get; set; } = new();

    public BlackjackTimings Timings { get; set; } = new();
}
