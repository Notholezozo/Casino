using Casino.Application.Abstractions;
using Casino.Infrastructure.Network;
using Casino.Infrastructure.Persistence;
using Casino.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Casino.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Casino";

    /// <param name="contentRootPath">Dossier de l'application : un chemin de base relatif y est rattaché.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Chaîne de connexion '{ConnectionStringName}' manquante.");

        services.AddDbContext<CasinoDbContext>(options =>
            options.UseSqlite(ResolveDataSource(connectionString, contentRootPath)));

        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<IRoundRepository, RoundRepository>();
        services.AddSingleton<ILanAddressProvider, LanAddressProvider>();

        return services;
    }

    /// <summary>
    /// Applique les migrations en attente, puis ferme les tables restées ouvertes :
    /// l'état des parties vit en mémoire et n'a pas survécu au redémarrage.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Casino.Database");

        await db.Database.MigrateAsync(ct);

        var closed = await scope.ServiceProvider.GetRequiredService<ITableRepository>()
            .CloseAllOpenAsync(DateTime.UtcNow, ct);
        if (closed > 0)
        {
            logger.LogInformation("{Count} table(s) restée(s) ouverte(s) avant le redémarrage ont été fermées", closed);
        }
    }

    private static string ResolveDataSource(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (!Path.IsPathRooted(builder.DataSource) && builder.DataSource != ":memory:")
        {
            builder.DataSource = Path.Combine(contentRootPath, builder.DataSource);
        }

        if (Path.GetDirectoryName(builder.DataSource) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ToString();
    }
}
