using Casino.Application.Tables;

namespace Casino.Api.Realtime;

/// <summary>Fait battre l'horloge du jeu : c'est elle qui déclenche les minuteurs de toutes les tables.</summary>
internal sealed class GameClockService(
    IServiceScopeFactory scopeFactory,
    ITableRegistry registry,
    ILogger<GameClockService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (registry.Sessions.Count == 0)
            {
                continue;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IGameClock>().TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erreur dans l'horloge du jeu");
            }
        }
    }
}
