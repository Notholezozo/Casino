using Casino.Application.Common;
using Casino.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.ErrorHandling;

/// <summary>
/// Traduit les erreurs métier en ProblemDetails : <c>detail</c> est le message à afficher,
/// <c>code</c> un identifiant stable pour le front. Les erreurs inattendues suivent le circuit par défaut (500).
/// </summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, code) = exception switch
        {
            GameRuleException rule => (StatusCodes.Status400BadRequest, rule.Code),
            AppException app => (ToStatus(app.Kind), app.Code),
            _ => (0, string.Empty),
        };

        if (status == 0)
        {
            return false;
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Detail = exception.Message,
                Extensions = { ["code"] = code },
            },
        });
    }

    private static int ToStatus(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest,
    };
}
