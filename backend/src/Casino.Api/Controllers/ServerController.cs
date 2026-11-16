using Casino.Application.Abstractions;
using Casino.Application.Tables.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Controllers;

[ApiController]
[Route("api/server")]
public sealed class ServerController(ILanAddressProvider lanAddresses) : ControllerBase
{
    /// <summary>
    /// Adresses du serveur sur le réseau local. L'écran table s'en sert pour afficher un lien
    /// et un QR code joignables depuis les téléphones quand il est lui-même ouvert en « localhost ».
    /// </summary>
    [HttpGet("network")]
    public ActionResult<ServerNetworkResponse> GetNetwork() =>
        new ServerNetworkResponse(lanAddresses.GetLanAddresses());
}
