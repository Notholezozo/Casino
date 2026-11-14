using Casino.Api.Auth;
using Casino.Application.Abstractions;
using Casino.Application.Blackjack.Contracts;
using Casino.Application.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Casino.Api.Realtime;

/// <summary>Noms des groupes SignalR et des événements serveur → client.</summary>
internal static class GameChannels
{
    public const string PlayerState = "PlayerState";
    public const string TableState = "TableState";
    public const string TableClosed = "TableClosed";
    public const string RemovedFromTable = "RemovedFromTable";

    /// <summary>Tous les écrans d'une table (admin et joueurs).</summary>
    public static string Table(string code) => $"table:{code.ToUpperInvariant()}";

    public static string Admin(string code) => $"admin:{code.ToUpperInvariant()}";

    public static string Player(Guid playerId) => $"player:{playerId}";
}

/// <summary>
/// Canal temps réel, à sens unique : le serveur pousse l'état, les commandes passent par l'API REST.
/// Le hub se contente de ranger chaque connexion dans ses groupes et de suivre qui est en ligne.
/// </summary>
[Authorize]
public sealed class GameHub(ITableService tables) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var session = Context.User!.GetSession();

        await Groups.AddToGroupAsync(Context.ConnectionId, GameChannels.Table(session.TableCode));
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            session.PlayerId is { } playerId ? GameChannels.Player(playerId) : GameChannels.Admin(session.TableCode));

        // Rediffuse l'état : le nouvel écran le reçoit, les autres voient le joueur en ligne.
        await tables.TrackConnectionAsync(session, connected: true);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await tables.TrackConnectionAsync(Context.User!.GetSession(), connected: false);
        await base.OnDisconnectedAsync(exception);
    }
}

internal sealed class SignalRGameNotifier(IHubContext<GameHub> hub) : IGameNotifier
{
    public Task SendPlayerStateAsync(Guid playerId, PlayerViewDto view) =>
        hub.Clients.Group(GameChannels.Player(playerId)).SendAsync(GameChannels.PlayerState, view);

    public Task SendTableStateAsync(string tableCode, TableViewDto view) =>
        hub.Clients.Group(GameChannels.Admin(tableCode)).SendAsync(GameChannels.TableState, view);

    public Task SendTableClosedAsync(string tableCode) =>
        hub.Clients.Group(GameChannels.Table(tableCode)).SendAsync(GameChannels.TableClosed);

    public Task SendRemovedAsync(Guid playerId) =>
        hub.Clients.Group(GameChannels.Player(playerId)).SendAsync(GameChannels.RemovedFromTable);
}
