using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Casino.Infrastructure.Persistence.Configurations;

internal sealed class GameTableConfiguration : IEntityTypeConfiguration<GameTable>
{
    public void Configure(EntityTypeBuilder<GameTable> builder)
    {
        builder.ToTable("Tables");
        builder.Property(t => t.Code).HasMaxLength(8).IsRequired();
        builder.Property(t => t.Game).HasConversion<string>().HasMaxLength(20);

        // Un code est réutilisable une fois la table fermée, d'où l'index non unique.
        builder.HasIndex(t => t.Code);
        builder.HasIndex(t => t.ClosedAtUtc);

        builder.HasMany(t => t.Players).WithOne().HasForeignKey(p => p.TableId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.Rounds).WithOne().HasForeignKey(r => r.TableId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        builder.Property(p => p.Name).HasMaxLength(16).IsRequired();
    }
}

internal sealed class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.ToTable("Rounds");
        builder.Property(r => r.DealerCards).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => new { r.TableId, r.Number });

        builder.HasMany(r => r.Hands).WithOne().HasForeignKey(h => h.RoundId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Transfers).WithOne().HasForeignKey(t => t.RoundId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RoundHandConfiguration : IEntityTypeConfiguration<RoundHand>
{
    public void Configure(EntityTypeBuilder<RoundHand> builder)
    {
        builder.ToTable("RoundHands");
        builder.Property(h => h.Cards).HasMaxLength(100).IsRequired();
        builder.Property(h => h.Outcome).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Unit).HasConversion<string>().HasMaxLength(10);

        builder.HasOne<Player>().WithMany().HasForeignKey(h => h.PlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RoundDrinkTransferConfiguration : IEntityTypeConfiguration<RoundDrinkTransfer>
{
    public void Configure(EntityTypeBuilder<RoundDrinkTransfer> builder)
    {
        builder.ToTable("RoundDrinkTransfers");
        builder.Property(t => t.Unit).HasConversion<string>().HasMaxLength(10);

        builder.HasOne<Player>().WithMany().HasForeignKey(t => t.FromPlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Player>().WithMany().HasForeignKey(t => t.ToPlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
