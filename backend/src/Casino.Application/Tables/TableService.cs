using Casino.Application.Abstractions;
using Casino.Application.Common;
using Casino.Application.Tables.Contracts;
using Casino.Domain.Blackjack;
using Casino.Domain.Cards;
using Casino.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casino.Application.Tables;

/// <summary>Cycle de vie des tables : ouverture, arrivée et départ des joueurs, connexions, fermeture.</summary>
public interface ITableService
{
    Task<CreateTableResponse> CreateAsync(CancellationToken ct = default);

    TableSummaryResponse GetSummary(string code);

    Task<JoinTableResponse> JoinAsync(string code, string? name, CancellationToken ct = default);

    /// <summary>Un joueur quitte la table, ou l'admin l'expulse.</summary>
    Task RemovePlayerAsync(SessionIdentity caller, string code, Guid playerId, CancellationToken ct = default);

    Task CloseAsync(SessionIdentity caller, string code, CancellationToken ct = default);

    /// <summary>Ferme une table sans vérifier l'appelant (nettoyage automatique).</summary>
    Task CloseAsync(TableSession session, CancellationToken ct = default);

    /// <summary>Signale qu'un écran vient de se connecter ou de se déconnecter, puis rediffuse l'état.</summary>
    Task TrackConnectionAsync(SessionIdentity identity, bool connected, CancellationToken ct = default);
}

internal sealed class TableService(
    ITableRegistry registry,
    ITableRepository tables,
    ISnapshotDispatcher dispatcher,
    IGameNotifier notifier,
    IRandomSource random,
    TimeProvider clock,
    IOptions<TablesOptions> tablesOptions,
    IOptions<BlackjackOptions> blackjackOptions,
    ILogger<TableService> logger) : ITableService
{
    private const int MaxNameLength = 16;

    public async Task<CreateTableResponse> CreateAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var options = blackjackOptions.Value;

        var session = registry.TryOpen(tablesOptions.Value.MaxOpenTables, code => new TableSession(
            Guid.NewGuid(),
            code,
            TableRegistry.NewToken(),
            new BlackjackGame(options.Rules, options.Timings, new Shoe(options.Rules.DeckCount, random), random),
            now));

        if (session is null)
        {
            throw new AppException(
                ErrorKind.Conflict,
                "too_many_tables",
                $"Il y a déjà {tablesOptions.Value.MaxOpenTables} tables ouvertes. Ferme une table avant d'en créer une autre.");
        }

        try
        {
            await tables.AddAsync(
                new GameTable
                {
                    Id = session.TableId,
                    Code = session.Code,
                    Game = GameType.Blackjack,
                    CreatedAtUtc = now.UtcDateTime,
                },
                ct);
        }
        catch
        {
            registry.Remove(session.Code);
            throw;
        }

        logger.LogInformation("Table {Code} ouverte", session.Code);
        return new CreateTableResponse(session.Code, session.AdminToken);
    }

    public TableSummaryResponse GetSummary(string code)
    {
        var session = GetSession(code);
        return session.Read(game => new TableSummaryResponse(
            session.Code,
            nameof(GameType.Blackjack),
            game.Seats.Count,
            game.Rules.MaxPlayers));
    }

    public async Task<JoinTableResponse> JoinAsync(string code, string? name, CancellationToken ct = default)
    {
        var session = GetSession(code);
        var playerName = (name ?? string.Empty).Trim();

        if (playerName.Length is 0 or > MaxNameLength)
        {
            throw new AppException(
                ErrorKind.Validation,
                "invalid_name",
                $"Choisis un pseudo de 1 à {MaxNameLength} caractères.");
        }

        var now = clock.GetUtcNow();
        var playerId = Guid.NewGuid();
        var token = TableRegistry.NewToken();

        var snapshot = session.Mutate(now, game =>
        {
            if (game.Seats.Count >= game.Rules.MaxPlayers)
            {
                throw new AppException(ErrorKind.Conflict, "table_full", "La table est complète.");
            }

            if (game.Seats.Any(s => string.Equals(s.Name, playerName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new AppException(ErrorKind.Conflict, "name_taken", "Ce pseudo est déjà pris à cette table.");
            }

            game.AddPlayer(playerId, playerName);
        });

        try
        {
            await tables.AddPlayerAsync(
                new Player
                {
                    Id = playerId,
                    TableId = session.TableId,
                    Name = playerName,
                    JoinedAtUtc = now.UtcDateTime,
                },
                ct);
        }
        catch
        {
            session.RemovePlayer(playerId, now);
            throw;
        }

        registry.RegisterPlayerToken(token, session.Code, playerId);
        await dispatcher.DispatchAsync(snapshot, ct);

        return new JoinTableResponse(session.Code, playerId, playerName, token);
    }

    public async Task RemovePlayerAsync(SessionIdentity caller, string code, Guid playerId, CancellationToken ct = default)
    {
        var session = GetSession(code);
        EnsureSameTable(caller, session);

        var isSelf = caller.Role == SessionRole.Player && caller.PlayerId == playerId;
        if (caller.Role != SessionRole.Admin && !isSelf)
        {
            throw new AppException(ErrorKind.Forbidden, "forbidden", "Seul l'écran de la table peut retirer un autre joueur.");
        }

        var now = clock.GetUtcNow();
        var snapshot = session.RemovePlayer(playerId, now);
        registry.RevokePlayerToken(playerId);

        await tables.MarkPlayerLeftAsync(playerId, now.UtcDateTime, ct);

        // Seul un joueur expulsé a besoin d'être prévenu : celui qui part de lui-même le sait déjà.
        if (!isSelf)
        {
            await notifier.SendRemovedAsync(playerId);
        }

        await dispatcher.DispatchAsync(snapshot, ct);
    }

    public Task CloseAsync(SessionIdentity caller, string code, CancellationToken ct = default)
    {
        var session = GetSession(code);
        EnsureSameTable(caller, session);

        if (caller.Role != SessionRole.Admin)
        {
            throw new AppException(ErrorKind.Forbidden, "forbidden", "Seul l'écran de la table peut la fermer.");
        }

        return CloseAsync(session, ct);
    }

    public async Task CloseAsync(TableSession session, CancellationToken ct = default)
    {
        registry.Remove(session.Code);
        await tables.CloseAsync(session.TableId, clock.GetUtcNow().UtcDateTime, ct);
        await notifier.SendTableClosedAsync(session.Code);
        logger.LogInformation("Table {Code} fermée", session.Code);
    }

    public async Task TrackConnectionAsync(SessionIdentity identity, bool connected, CancellationToken ct = default)
    {
        // La table peut avoir été fermée entre-temps : une déconnexion tardive n'est pas une erreur.
        if (!registry.TryGet(identity.TableCode, out var session))
        {
            return;
        }

        var now = clock.GetUtcNow();
        var snapshot = identity is { Role: SessionRole.Player, PlayerId: { } playerId }
            ? session.TrackPlayerConnection(playerId, connected, now)
            : session.TrackAdminConnection(connected, now);

        if (snapshot is not null)
        {
            await dispatcher.DispatchAsync(snapshot, ct);
        }
    }

    private TableSession GetSession(string code) =>
        registry.TryGet(code, out var session) ? session : throw AppException.TableNotFound();

    private static void EnsureSameTable(SessionIdentity caller, TableSession session)
    {
        if (!string.Equals(caller.TableCode, session.Code, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(ErrorKind.Forbidden, "forbidden", "Ce jeton n'appartient pas à cette table.");
        }
    }
}
