using Casino.Application.Abstractions;
using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Persistence.Repositories;

internal sealed class TableRepository(CasinoDbContext db) : ITableRepository
{
    public async Task AddAsync(GameTable table, CancellationToken ct = default)
    {
        db.Tables.Add(table);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddPlayerAsync(Player player, CancellationToken ct = default)
    {
        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
    }

    public Task MarkPlayerLeftAsync(Guid playerId, DateTime leftAtUtc, CancellationToken ct = default) =>
        db.Players
            .Where(p => p.Id == playerId)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.LeftAtUtc, leftAtUtc), ct);

    public Task CloseAsync(Guid tableId, DateTime closedAtUtc, CancellationToken ct = default) =>
        db.Tables
            .Where(t => t.Id == tableId && t.ClosedAtUtc == null)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.ClosedAtUtc, closedAtUtc), ct);

    public Task<int> CloseAllOpenAsync(DateTime closedAtUtc, CancellationToken ct = default) =>
        db.Tables
            .Where(t => t.ClosedAtUtc == null)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.ClosedAtUtc, closedAtUtc), ct);
}
