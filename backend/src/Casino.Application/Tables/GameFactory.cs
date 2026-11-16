using Casino.Application.Blackjack;
using Casino.Application.Common;
using Casino.Application.Poker;
using Casino.Application.Tables.Contracts;
using Casino.Domain.Blackjack;
using Casino.Domain.Cards;
using Casino.Domain.Common;
using Casino.Domain.Entities;
using Casino.Domain.Poker;
using Microsoft.Extensions.Options;

namespace Casino.Application.Tables;

/// <summary>Un jeu prêt à être posé sur une table : son moteur, ses vues et les réglages retenus.</summary>
public sealed record GameSetup(ITableGame Game, IGameViewBuilder Views, TableSettings Settings);

/// <summary>
/// Fabrique le moteur du jeu choisi à la création d'une table.
/// Ajouter un jeu, c'est ajouter un cas ici (et son moteur, ses vues, son contrôleur).
/// </summary>
public interface IGameFactory
{
    GameSetup Create(CreateTableRequest request);
}

internal sealed class GameFactory(
    IRandomSource random,
    IOptions<BlackjackOptions> blackjackOptions,
    IOptions<PokerOptions> pokerOptions) : IGameFactory
{
    public GameSetup Create(CreateTableRequest request)
    {
        // Les mises de la configuration du blackjack servent de valeurs par défaut et de bornes
        // pour toutes les tables : une gorgée reste une gorgée, quel que soit le jeu.
        var defaults = blackjackOptions.Value.Rules;
        var minBet = request.MinBet ?? defaults.MinBet;
        var maxBet = request.MaxBet ?? defaults.MaxBet;

        if (minBet < defaults.MinBet || maxBet > defaults.MaxBet || minBet > maxBet)
        {
            throw new AppException(
                ErrorKind.Validation,
                "invalid_bet_limits",
                $"Les mises doivent rester entre {defaults.MinBet} et {defaults.MaxBet} gorgées, avec un minimum inférieur ou égal au maximum.");
        }

        return request.Game switch
        {
            GameType.Blackjack => CreateBlackjack(minBet, maxBet, request.AllowCulSec),
            GameType.Poker => CreatePoker(minBet, maxBet),
            _ => throw new AppException(ErrorKind.Validation, "unknown_game", "Ce jeu n'existe pas."),
        };
    }

    private GameSetup CreateBlackjack(int minBet, int maxBet, bool allowCulSec)
    {
        var options = blackjackOptions.Value;
        var rules = options.Rules with { MinBet = minBet, MaxBet = maxBet, AllowCulSec = allowCulSec };
        var game = new BlackjackGame(rules, options.Timings, new Shoe(rules.DeckCount, random), random);

        return new GameSetup(
            game,
            new BlackjackViewBuilder(game),
            new TableSettings(GameType.Blackjack, minBet, maxBet, allowCulSec));
    }

    /// <summary>
    /// Au poker, la mise minimum de la table donne les blindes (petite = minimum, grosse = le double)
    /// et la plus petite relance ; la mise maximum plafonne chaque relance. Pas de cul sec.
    /// </summary>
    private GameSetup CreatePoker(int minBet, int maxBet)
    {
        var options = pokerOptions.Value;
        var rules = options.Rules with
        {
            SmallBlind = minBet,
            BigBlind = minBet * 2,
            MinRaise = minBet,
            MaxRaise = maxBet,
        };

        // Un jeu de 52 cartes neuf et mélangé à chaque main.
        var game = new PokerGame(rules, options.Timings, () => new Shoe(1, random), random);

        return new GameSetup(
            game,
            new PokerViewBuilder(game),
            new TableSettings(GameType.Poker, minBet, maxBet, AllowCulSec: false));
    }
}
