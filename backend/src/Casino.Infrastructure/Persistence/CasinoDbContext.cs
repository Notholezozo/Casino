using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Persistence;

public sealed class CasinoDbContext(DbContextOptions<CasinoDbContext> options) : DbContext(options)
{
    public DbSet<GameTable> Tables => Set<GameTable>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Round> Rounds => Set<Round>();

    public DbSet<RoundHand> RoundHands => Set<RoundHand>();

    public DbSet<RoundDrinkTransfer> RoundDrinkTransfers => Set<RoundDrinkTransfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CasinoDbContext).Assembly);
}
