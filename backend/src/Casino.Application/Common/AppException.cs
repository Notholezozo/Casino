namespace Casino.Application.Common;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
}

/// <summary>
/// Erreur applicative attendue (table introuvable, table pleine…). L'API la traduit en code HTTP ;
/// le message est destiné à être affiché tel quel à l'utilisateur.
/// </summary>
public sealed class AppException(ErrorKind kind, string code, string message) : Exception(message)
{
    public ErrorKind Kind { get; } = kind;

    public string Code { get; } = code;

    public static AppException TableNotFound() =>
        new(ErrorKind.NotFound, "table_not_found", "Cette table n'existe pas ou a été fermée.");
}
