using Casino.Api.Auth;
using Casino.Application.Blackjack.Contracts;
using Casino.Application.Poker;
using Casino.Application.Poker.Contracts;
using Casino.Application.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Controllers;

/// <summary>
/// Commandes du poker. La table et le joueur sont déduits du jeton : un joueur ne peut agir que pour lui-même.
/// Les changements d'état repartent vers tous les écrans par le hub SignalR.
/// </summary>
[ApiController]
[Route("api/poker")]
[Authorize]
public sealed class PokerController(IPokerService poker, ITableService tables) : ControllerBase
{
    /// <summary>État courant : vue joueur ou vue table selon le jeton présenté.</summary>
    [HttpGet("state")]
    [ProducesResponseType<PokerPlayerViewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PokerTableViewDto>(StatusCodes.Status200OK)]
    public IActionResult GetState() => Ok(tables.GetState(User.GetSession()));

    /// <summary>Fold, Check, Call ou Raise.</summary>
    [HttpPost("action")]
    [Authorize(Roles = SessionAuth.PlayerRole)]
    public async Task<IActionResult> Act(PokerActionRequest request, CancellationToken ct)
    {
        await poker.ActAsync(User.GetSession(), request.Action, request.RaiseBy, ct);
        return NoContent();
    }

    /// <summary>Le gagnant répartit sa part du pot entre les autres joueurs.</summary>
    [HttpPost("distribution")]
    [Authorize(Roles = SessionAuth.PlayerRole)]
    public async Task<IActionResult> Distribute(DistributeRequest request, CancellationToken ct)
    {
        await poker.DistributeAsync(User.GetSession(), request.Allocations, ct);
        return NoContent();
    }
}
