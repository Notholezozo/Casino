using Casino.Api.Auth;
using Casino.Application.Tables;
using Casino.Application.Tables.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Controllers;

[ApiController]
[Route("api/tables")]
public sealed class TablesController(ITableService tables) : ControllerBase
{
    /// <summary>Ouvre une table. L'appelant devient son écran admin.</summary>
    [HttpPost]
    [ProducesResponseType<CreateTableResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var response = await tables.CreateAsync(ct);
        return CreatedAtAction(nameof(Get), new { code = response.Code }, response);
    }

    /// <summary>Permet de vérifier un code avant de rejoindre.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TableSummaryResponse> Get(string code) => tables.GetSummary(code);

    [HttpDelete("{code}")]
    [Authorize(Roles = SessionAuth.AdminRole)]
    public async Task<IActionResult> Close(string code, CancellationToken ct)
    {
        await tables.CloseAsync(User.GetSession(), code, ct);
        return NoContent();
    }

    /// <summary>Rejoint la table en invité.</summary>
    [HttpPost("{code}/players")]
    [ProducesResponseType<JoinTableResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(string code, JoinTableRequest request, CancellationToken ct)
    {
        var response = await tables.JoinAsync(code, request.Name, ct);
        return CreatedAtAction(nameof(Get), new { code = response.Code }, response);
    }

    /// <summary>Quitter la table (le joueur lui-même) ou expulser un joueur (admin).</summary>
    [HttpDelete("{code}/players/{playerId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemovePlayer(string code, Guid playerId, CancellationToken ct)
    {
        await tables.RemovePlayerAsync(User.GetSession(), code, playerId, ct);
        return NoContent();
    }
}
