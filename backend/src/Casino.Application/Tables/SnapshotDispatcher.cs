using Casino.Application.Abstractions;
using Casino.Domain.Blackjack;
using Casino.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Tables;

/// <summary>Donne suite à une modification de table : historise la manche terminée, puis diffuse l'état.</summary>
public interface ISnapshotDispatcher
{
    Task DispatchAsync(TableSnapshot snapshot, CancellationToken ct = default);
}

internal sealed class SnapshotDispatcher(
    IGameNotifier notifier,
    IRoundRepository rounds,
    TimeProvider clock,
    ILogger<SnapshotDispatcher> logger) : ISnapshotDispatcher
{
    public async Task DispatchAsync(TableSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.CompletedRound is { Hands.Count: > 0 } round)
        {
            await SaveRoundAsync(snapshot.TableId, round, ct);
        }

        var sends = snapshot.Players
            .Select(p => notifier.SendPlayerStateAsync(p.Key, p.Value))
            .Append(notifier.SendTableStateAsync(snapshot.Code, snapshot.Table));

        await Task.WhenAll(sends);
    }

    private async Task SaveRoundAsync(Guid tableId, RoundSummary summary, CancellationToken ct)
    {
        var round = new Round
        {
            Id = Guid.NewGuid(),
            TableId = tableId,
            Number = summary.RoundNumber,
            EndedAtUtc = clock.GetUtcNow().UtcDateTime,
            DealerCards = string.Join(' ', summary.DealerCards),
            DealerTotal = summary.DealerTotal,
            Hands = summary.Hands.Select(h => new RoundHand
            {
                Id = Guid.NewGuid(),
                PlayerId = h.PlayerId,
                Cards = string.Join(' ', h.Cards),
                Total = h.Total,
                Bet = h.Bet,
                Unit = h.Unit,
                Outcome = h.Outcome,
            }).ToList(),
            Transfers = summary.Transfers.Select(t => new RoundDrinkTransfer
            {
                Id = Guid.NewGuid(),
                FromPlayerId = t.FromPlayerId,
                ToPlayerId = t.ToPlayerId,
                Amount = t.Amount,
                Unit = t.Unit,
            }).ToList(),
        };

        try
        {
            await rounds.AddAsync(round, summary.Totals, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // L'historique est secondaire : une panne de base ne doit pas interrompre la partie en cours.
            logger.LogError(ex, "Impossible d'enregistrer la manche {Round} de la table {TableId}", summary.RoundNumber, tableId);
        }
    }
}
