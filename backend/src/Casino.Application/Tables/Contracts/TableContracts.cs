namespace Casino.Application.Tables.Contracts;

/// <summary>Réponse à la création d'une table. Le jeton admin n'est renvoyé qu'ici.</summary>
public sealed record CreateTableResponse(string Code, string AdminToken);

/// <summary>Informations publiques d'une table, pour valider un code avant de rejoindre.</summary>
public sealed record TableSummaryResponse(string Code, string Game, int PlayerCount, int MaxPlayers);

public sealed record JoinTableRequest(string Name);

/// <summary>Identité invité du joueur. Le jeton sert ensuite de Bearer pour l'API et le hub.</summary>
public sealed record JoinTableResponse(string Code, Guid PlayerId, string Name, string Token);
