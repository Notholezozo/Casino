using Casino.Application.Common;
using Casino.Domain.Blackjack;
using Casino.Domain.Common;
using Casino.Domain.Entities;

namespace Casino.Application.Tables;

/// <summary>Réglages choisis à la création de la table.</summary>
public sealed record TableSettings(GameType Game, int MinBet, int MaxBet, bool AllowCulSec);

/// <summary>
/// Projette l'état d'un moteur de jeu vers les vues envoyées aux écrans. Une implémentation par jeu :
/// c'est elle qui garantit qu'un téléphone ne reçoit que ce que son joueur a le droit de voir.
/// </summary>
public interface IGameViewBuilder
{
    object BuildTableView(string tableCode, long version, DateTimeOffset now);

    /// <summary>Vue d'un joueur, ou null s'il n'est pas assis à la table.</summary>
    object? BuildPlayerView(Guid playerId, string tableCode, long version, DateTimeOffset now);
}

/// <summary>Photo cohérente d'une table après une modification : ce qu'il faut diffuser et enregistrer.</summary>
public sealed record TableSnapshot(
    Guid TableId,
    string Code,
    object Table,
    IReadOnlyDictionary<Guid, object> Players,
    RoundSummary? CompletedRound);

/// <summary>
/// Table ouverte, en mémoire : le moteur de jeu, les jetons d'accès et les connexions.
/// Elle ne connaît pas les règles : tout accès au moteur passe par <see cref="Mutate{TGame}"/>
/// ou <see cref="Read{T}"/>, qui le sérialisent.
/// </summary>
public sealed class TableSession
{
    private readonly Lock _gate = new();
    private readonly ITableGame _game;
    private readonly IGameViewBuilder _views;
    private readonly Dictionary<Guid, int> _playerConnections = [];
    private int _adminConnections;
    private long _version;

    public TableSession(
        Guid tableId,
        string code,
        string adminToken,
        TableSettings settings,
        ITableGame game,
        IGameViewBuilder views,
        DateTimeOffset now)
    {
        TableId = tableId;
        Code = code;
        AdminToken = adminToken;
        Settings = settings;
        _game = game;
        _views = views;
        IdleSince = now;
    }

    public Guid TableId { get; }

    public string Code { get; }

    public string AdminToken { get; }

    public TableSettings Settings { get; }

    /// <summary>Instant depuis lequel plus aucun écran n'est connecté ; null si au moins un l'est.</summary>
    public DateTimeOffset? IdleSince
    {
        get
        {
            lock (_gate)
            {
                return field;
            }
        }

        private set;
    }

    /// <summary>
    /// Exécute une modification du jeu et renvoie la photo à diffuser.
    /// Échoue si la table ne joue pas au jeu attendu (commande de blackjack sur une table de poker…).
    /// </summary>
    public TableSnapshot Mutate<TGame>(DateTimeOffset now, Action<TGame> action)
        where TGame : class, ITableGame
    {
        lock (_gate)
        {
            action(_game as TGame ?? throw WrongGame());
            return Capture(now);
        }
    }

    /// <summary>Fait avancer l'horloge du jeu ; renvoie une photo seulement si quelque chose a changé.</summary>
    public TableSnapshot? Tick(DateTimeOffset now)
    {
        lock (_gate)
        {
            return _game.Tick(now) ? Capture(now) : null;
        }
    }

    public T Read<T>(Func<ITableGame, T> query)
    {
        lock (_gate)
        {
            return query(_game);
        }
    }

    public object GetTableView(DateTimeOffset now)
    {
        lock (_gate)
        {
            return _views.BuildTableView(Code, _version, now);
        }
    }

    public object? GetPlayerView(Guid playerId, DateTimeOffset now)
    {
        lock (_gate)
        {
            return _views.BuildPlayerView(playerId, Code, _version, now);
        }
    }

    /// <summary>Compte un écran de plus (ou de moins) pour l'admin. Un joueur peut avoir plusieurs onglets.</summary>
    public TableSnapshot TrackAdminConnection(bool connected, DateTimeOffset now)
    {
        lock (_gate)
        {
            _adminConnections = Math.Max(0, _adminConnections + (connected ? 1 : -1));
            UpdateIdle(now);
            return Capture(now);
        }
    }

    public TableSnapshot? TrackPlayerConnection(Guid playerId, bool connected, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (_game.Players.All(p => p.Id != playerId))
            {
                return null;
            }

            var count = Math.Max(0, _playerConnections.GetValueOrDefault(playerId) + (connected ? 1 : -1));
            _playerConnections[playerId] = count;
            _game.SetConnected(playerId, count > 0, now);
            UpdateIdle(now);
            return Capture(now);
        }
    }

    public TableSnapshot RemovePlayer(Guid playerId, DateTimeOffset now)
    {
        lock (_gate)
        {
            _game.RemovePlayer(playerId, now);
            _playerConnections.Remove(playerId);
            UpdateIdle(now);
            return Capture(now);
        }
    }

    private void UpdateIdle(DateTimeOffset now)
    {
        var anyone = _adminConnections > 0 || _playerConnections.Values.Any(c => c > 0);
        IdleSince = anyone ? null : IdleSince ?? now;
    }

    private TableSnapshot Capture(DateTimeOffset now)
    {
        _version++;
        return new TableSnapshot(
            TableId,
            Code,
            _views.BuildTableView(Code, _version, now),
            _game.Players.ToDictionary(
                player => player.Id,
                player => _views.BuildPlayerView(player.Id, Code, _version, now)!),
            _game.TakeCompletedRound());
    }

    private static AppException WrongGame() =>
        new(ErrorKind.Validation, "wrong_game", "Cette action n'existe pas dans le jeu de cette table.");
}
