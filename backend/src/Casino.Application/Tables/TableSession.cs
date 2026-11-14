using Casino.Application.Blackjack;
using Casino.Application.Blackjack.Contracts;
using Casino.Domain.Blackjack;

namespace Casino.Application.Tables;

/// <summary>Photo cohérente d'une table après une modification : ce qu'il faut diffuser et enregistrer.</summary>
public sealed record TableSnapshot(
    Guid TableId,
    string Code,
    TableViewDto Table,
    IReadOnlyDictionary<Guid, PlayerViewDto> Players,
    RoundSummary? CompletedRound);

/// <summary>
/// Table ouverte, en mémoire : le moteur de jeu, les jetons d'accès et les connexions.
/// Tout accès au moteur passe par <see cref="Mutate"/> ou <see cref="Read{T}"/>, qui le sérialisent.
/// </summary>
public sealed class TableSession
{
    private readonly Lock _gate = new();
    private readonly BlackjackGame _game;
    private readonly Dictionary<Guid, int> _playerConnections = [];
    private int _adminConnections;
    private long _version;

    public TableSession(Guid tableId, string code, string adminToken, BlackjackGame game, DateTimeOffset now)
    {
        TableId = tableId;
        Code = code;
        AdminToken = adminToken;
        _game = game;
        IdleSince = now;
    }

    public Guid TableId { get; }

    public string Code { get; }

    public string AdminToken { get; }

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

    /// <summary>Exécute une modification du jeu et renvoie la photo à diffuser.</summary>
    public TableSnapshot Mutate(DateTimeOffset now, Action<BlackjackGame> action)
    {
        lock (_gate)
        {
            action(_game);
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

    public T Read<T>(Func<BlackjackGame, T> query)
    {
        lock (_gate)
        {
            return query(_game);
        }
    }

    public TableViewDto GetTableView(DateTimeOffset now) =>
        Read(game => BlackjackViewMapper.ToTableView(game, Code, _version, now));

    public PlayerViewDto? GetPlayerView(Guid playerId, DateTimeOffset now) =>
        Read(game => game.Seats.FirstOrDefault(s => s.PlayerId == playerId) is { } seat
            ? BlackjackViewMapper.ToPlayerView(game, seat, Code, _version, now)
            : null);

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
            if (_game.Seats.All(s => s.PlayerId != playerId))
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
            BlackjackViewMapper.ToTableView(_game, Code, _version, now),
            _game.Seats.ToDictionary(
                seat => seat.PlayerId,
                seat => BlackjackViewMapper.ToPlayerView(_game, seat, Code, _version, now)),
            _game.TakeCompletedRound());
    }
}
