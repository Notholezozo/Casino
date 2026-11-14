namespace Casino.Domain.Blackjack;

/// <summary>Règles paramétrables d'une table de blackjack.</summary>
public sealed record BlackjackRules
{
    public int DeckCount { get; init; } = 4;

    public int MaxPlayers { get; init; } = 6;

    /// <summary>Mise minimale en gorgées pour participer (0 reste toujours possible et signifie « je passe »).</summary>
    public int MinBet { get; init; } = 1;

    public int MaxBet { get; init; } = 10;

    /// <summary>Nombre maximal de mains par joueur après séparations.</summary>
    public int MaxHandsPerPlayer { get; init; } = 4;

    /// <summary>Faux = le croupier reste sur tout 17 (règle classique).</summary>
    public bool DealerHitsSoft17 { get; init; }

    /// <summary>Gorgées distribuées pour un blackjack naturel : mise × 3/2, arrondi au-dessus.</summary>
    public int BlackjackPayout(int bet) => (bet * 3 + 1) / 2;
}

/// <summary>Durées des phases automatiques.</summary>
public sealed record BlackjackTimings
{
    /// <summary>Temps pour miser, compté à partir de la première mise posée.</summary>
    public TimeSpan Betting { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>Temps par décision ; à l'échéance la main reste.</summary>
    public TimeSpan Turn { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>Délai entre deux gestes du croupier (retourner, tirer, annoncer).</summary>
    public TimeSpan DealerStep { get; init; } = TimeSpan.FromSeconds(1.2);

    /// <summary>Temps pour distribuer ses gorgées ; à l'échéance la répartition est automatique.</summary>
    public TimeSpan Distribution { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Durée d'affichage du bilan avant la manche suivante.</summary>
    public TimeSpan Results { get; init; } = TimeSpan.FromSeconds(10);
}
