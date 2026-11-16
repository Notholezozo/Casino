using Casino.Domain.Entities;

namespace Casino.Application.Tables.Contracts;

/// <summary>
/// Réglages choisis par le créateur de la table. Un champ omis prend la valeur par défaut de la configuration.
/// </summary>
/// <param name="MinBet">Mise minimale en gorgées. Au poker : la petite blinde (la grosse vaut le double).</param>
/// <param name="MaxBet">Mise maximale en gorgées. Au poker : la plus grosse relance possible.</param>
/// <param name="AllowCulSec">Blackjack seulement : autorise la mise « cul sec », au-dessus de la mise maximale.</param>
/// <param name="Game">Jeu de la table : "Blackjack" (par défaut) ou "Poker".</param>
public sealed record CreateTableRequest(
    int? MinBet = null,
    int? MaxBet = null,
    bool AllowCulSec = false,
    GameType Game = GameType.Blackjack);

/// <summary>Réponse à la création d'une table. Le jeton admin n'est renvoyé qu'ici.</summary>
public sealed record CreateTableResponse(string Code, string AdminToken, GameType Game);

/// <summary>Informations publiques d'une table, pour valider un code avant de rejoindre.</summary>
public sealed record TableSummaryResponse(
    string Code,
    GameType Game,
    int PlayerCount,
    int MaxPlayers,
    int MinBet,
    int MaxBet,
    bool AllowCulSec);

/// <summary>Adresses IPv4 du serveur sur le réseau local, la plus probable en premier (vide si aucune).</summary>
public sealed record ServerNetworkResponse(IReadOnlyList<string> LanAddresses);

public sealed record JoinTableRequest(string Name);

/// <summary>
/// Identité invité du joueur. Le jeton sert ensuite de Bearer pour l'API et le hub ;
/// le jeu indique au front quelle interface afficher.
/// </summary>
public sealed record JoinTableResponse(string Code, Guid PlayerId, string Name, string Token, GameType Game);
