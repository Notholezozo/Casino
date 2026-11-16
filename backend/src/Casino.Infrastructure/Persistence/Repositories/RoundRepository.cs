using Casino.Application.Abstractions;
using Casino.Domain.Blackjack;
using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Persistence.Repositories;

internal sealed class RoundRepository(CasinoDbContext db) : IRoundRepository
{
    public async Task AddAsync(Round round, IReadOnlyList<PlayerTotals> totals, CancellationToken ct = default)
    {
        // Manche et cumuls sont enregistrés ensemble : l'historique reste cohérent avec les totaux.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        db.Rounds.Add(round);
        await db.SaveChangesAsync(ct);

        foreach (var total in totals)
        {
            await db.Players
                .Where(p => p.Id == total.PlayerId)
                .ExecuteUpdateAsync(
                    set => set
                        .SetProperty(p => p.TotalDrunk, total.Totals.SipsDrunk)
                        .SetProperty(p => p.TotalGiven, total.Totals.SipsGiven)
                        .SetProperty(p => p.TotalCulsSecsDrunk, total.Totals.CulsSecsDrunk)
                        .SetProperty(p => p.TotalCulsSecsGiven, total.Totals.CulsSecsGiven),
                    ct);
        }

        await transaction.CommitAsync(ct);
    }
}
