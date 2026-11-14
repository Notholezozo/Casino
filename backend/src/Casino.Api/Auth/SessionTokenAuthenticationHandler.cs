using System.Security.Claims;
using System.Text.Encodings.Web;
using Casino.Application.Tables;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Casino.Api.Auth;

public static class SessionAuth
{
    public const string Scheme = "SessionToken";

    public const string AdminRole = nameof(SessionRole.Admin);

    public const string PlayerRole = nameof(SessionRole.Player);

    public const string TableCodeClaim = "table_code";

    public const string PlayerIdClaim = "player_id";
}

/// <summary>
/// Authentification invité : le client présente le jeton opaque reçu à la création de la table
/// ou en la rejoignant. Pas de compte ; le jeton cesse d'être valable quand la table ferme.
/// </summary>
public sealed class SessionTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITableRegistry registry) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadToken();
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var session = registry.Resolve(token);
        if (session is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Jeton inconnu ou table fermée."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, session.Role.ToString()),
            new(SessionAuth.TableCodeClaim, session.TableCode),
        };
        if (session.PlayerId is { } playerId)
        {
            claims.Add(new Claim(SessionAuth.PlayerIdClaim, playerId.ToString()));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    private string? ReadToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return header[BearerPrefix.Length..].Trim();
        }

        // Un WebSocket ne peut pas porter d'en-tête : SignalR passe le jeton dans l'URL.
        return Request.Path.StartsWithSegments("/hubs") ? Request.Query["access_token"].ToString() : null;
    }
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reconstruit l'identité de session à partir des claims posés par le handler.</summary>
    public static SessionIdentity GetSession(this ClaimsPrincipal user)
    {
        var role = Enum.Parse<SessionRole>(user.FindFirstValue(ClaimTypes.Role)!);
        var tableCode = user.FindFirstValue(SessionAuth.TableCodeClaim)!;
        var playerId = user.FindFirstValue(SessionAuth.PlayerIdClaim);

        return new SessionIdentity(role, tableCode, playerId is null ? null : Guid.Parse(playerId));
    }
}
