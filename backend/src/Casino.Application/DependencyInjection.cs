using Casino.Application.Blackjack;
using Casino.Application.Poker;
using Casino.Application.Tables;
using Casino.Domain.Cards;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Casino.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IRandomSource, CryptoRandomSource>();

        // L'état des parties vit en mémoire : un seul registre pour tout le processus.
        services.AddSingleton<ITableRegistry, TableRegistry>();

        services.AddScoped<ISnapshotDispatcher, SnapshotDispatcher>();
        services.AddScoped<ITableService, TableService>();
        services.AddScoped<IGameFactory, GameFactory>();
        services.AddScoped<IGameCommandRunner, GameCommandRunner>();
        services.AddScoped<IBlackjackService, BlackjackService>();
        services.AddScoped<IPokerService, PokerService>();
        services.AddScoped<IGameClock, GameClock>();

        return services;
    }
}
