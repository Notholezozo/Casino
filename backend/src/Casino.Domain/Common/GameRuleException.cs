namespace Casino.Domain.Common;

/// <summary>
/// Action refusée par les règles du jeu (pas son tour, mise invalide…).
/// Le message est destiné à être affiché tel quel au joueur.
/// </summary>
public sealed class GameRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
