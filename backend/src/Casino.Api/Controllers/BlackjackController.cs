using Casino.Api.Auth;
using Casino.Application.Blackjack;
using Casino.Application.Blackjack.Contracts;
using Casino.Application.Tables;
using Casino.Domain.Blackjack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Controllers;

/// <summary>
/// Commandes de jeu. La table et le joueur sont déduits du jeton : un joueur ne peut agir que pour lui-même.
/// Les changements d'état repartent vers tous les écrans par le hub SignalR.
/// </summary>
[ApiController]
[Route("api/blackjack")]
[Authorize]
public sealed class BlackjackController(IBlackjackService blackjack, ITableService tables) : ControllerBase
{
    /// <summary>État courant : vue joueur ou vue table selon le jeton présenté.</summary>
    [HttpGet("state")]
    [ProducesResponseType<PlayerViewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<TableViewDto>(StatusCodes.Status200OK)]
    public IActionResult GetState() => Ok(tables.GetState(User.GetSession()));

    [HttpPost("bet")]
    [Authorize(Roles = SessionAuth.PlayerRole)]
    public async Task<IActionResult> PlaceBet(PlaceBetRequest request, CancellationToken ct)
    {
        var bet = request.CulSec ? Bet.CulSec : Bet.Sips(request.Sips);
        await blackjack.PlaceBetAsync(User.GetSession(), bet, ct);
        return NoContent();
    }

    [HttpPost("action")]
    [Authorize(Roles = SessionAuth.PlayerRole)]
    public async Task<IActionResult> Act(PlayerActionRequest request, CancellationToken ct)
    {
        await blackjack.ActAsync(User.GetSession(), request.Action, ct);
        return NoContent();
    }

    [HttpPost("distribution")]
    [Authorize(Roles = SessionAuth.PlayerRole)]
    public async Task<IActionResult> Distribute(DistributeRequest request, CancellationToken ct)
    {
        await blackjack.DistributeAsync(User.GetSession(), request.Allocations, ct);
        return NoContent();
    }
}
