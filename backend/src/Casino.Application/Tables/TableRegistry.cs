using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Casino.Application.Tables;

public enum SessionRole
{
    Admin,
    Player,
}

/// <summary>Qui se cache derrière un jeton : l'écran table (admin) ou un joueur invité.</summary>
public sealed record SessionIdentity(SessionRole Role, string TableCode, Guid? PlayerId);

/// <summary>
/// Registre en mémoire des tables ouvertes et des jetons d'accès. Singleton.
/// L'état de jeu vit ici ; la base ne garde que les tables, les joueurs et l'historique.
/// </summary>
public interface ITableRegistry
{
    IReadOnlyCollection<TableSession> Sessions { get; }

    bool TryGet(string code, [NotNullWhen(true)] out TableSession? session);

    /// <summary>Ouvre une table sous un code libre, ou renvoie null si la limite est atteinte.</summary>
    TableSession? TryOpen(int maxOpenTables, Func<string, TableSession> create);

    void Remove(string code);

    void RegisterPlayerToken(string token, string tableCode, Guid playerId);

    void RevokePlayerToken(Guid playerId);

    SessionIdentity? Resolve(string token);
}

public sealed class TableRegistry : ITableRegistry
{
    // Lettres sans ambiguïté à l'oral ou à l'écran (pas de I, O, etc.).
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int CodeLength = 4;

    private readonly Lock _openLock = new();
    private readonly ConcurrentDictionary<string, TableSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SessionIdentity> _tokens = new();

    public IReadOnlyCollection<TableSession> Sessions => _sessions.Values.ToList();

    public bool TryGet(string code, [NotNullWhen(true)] out TableSession? session) =>
        _sessions.TryGetValue(code, out session);

    public TableSession? TryOpen(int maxOpenTables, Func<string, TableSession> create)
    {
        lock (_openLock)
        {
            if (_sessions.Count >= maxOpenTables)
            {
                return null;
            }

            string code;
            do
            {
                code = RandomNumberGenerator.GetString(CodeAlphabet, CodeLength);
            }
            while (_sessions.ContainsKey(code));

            var session = create(code);
            _sessions[code] = session;
            _tokens[session.AdminToken] = new SessionIdentity(SessionRole.Admin, code, null);
            return session;
        }
    }

    public void Remove(string code)
    {
        if (!_sessions.TryRemove(code, out _))
        {
            return;
        }

        foreach (var (token, _) in _tokens.Where(t => string.Equals(t.Value.TableCode, code, StringComparison.OrdinalIgnoreCase)))
        {
            _tokens.TryRemove(token, out _);
        }
    }

    public void RegisterPlayerToken(string token, string tableCode, Guid playerId) =>
        _tokens[token] = new SessionIdentity(SessionRole.Player, tableCode, playerId);

    public void RevokePlayerToken(Guid playerId)
    {
        foreach (var (token, _) in _tokens.Where(t => t.Value.PlayerId == playerId))
        {
            _tokens.TryRemove(token, out _);
        }
    }

    public SessionIdentity? Resolve(string token) => _tokens.GetValueOrDefault(token);

    /// <summary>Jeton opaque, imprévisible, sûr dans une URL.</summary>
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
}
