using Casino.Domain.Cards;
using Casino.Domain.Common;

namespace Casino.Domain.Blackjack;

/// <summary>
/// Moteur d'une table de blackjack : machine à états d'une manche
/// (mises → tours des joueurs → croupier → distribution des gorgées → bilan).
/// <para>
/// La classe est pure : aucune horloge ni E/S. L'appelant fournit l'instant courant et appelle
/// <see cref="Tick"/> régulièrement pour faire avancer les phases minutées. Elle n'est pas
/// thread-safe ; l'appelant sérialise les accès à une même table.
/// </para>
/// </summary>
public sealed class BlackjackGame : ITableGame
{
    private readonly BlackjackTimings _timings;
    private readonly IShoe _shoe;
    private readonly IRandomSource _random;
    private readonly List<Seat> _seats = [];
    private readonly List<Card> _dealerCards = [];
    private readonly List<DrinkTransfer> _transfers = [];
    private RoundSummary? _completedRound;

    public BlackjackGame(BlackjackRules rules, BlackjackTimings timings, IShoe shoe, IRandomSource random)
    {
        Rules = rules;
        _timings = timings;
        _shoe = shoe;
        _random = random;
    }

    public BlackjackRules Rules { get; }

    public GamePhase Phase { get; private set; } = GamePhase.Betting;

    /// <summary>Échéance de la phase en cours ; null quand rien n'est minuté.</summary>
    public DateTimeOffset? Deadline { get; private set; }

    public int RoundNumber { get; private set; }

    public IReadOnlyList<Seat> Seats => _seats;

    public IReadOnlyList<Card> DealerCards => _dealerCards;

    /// <summary>Faux tant que la seconde carte du croupier est face cachée.</summary>
    public bool DealerHoleRevealed { get; private set; }

    public Seat? ActiveSeat { get; private set; }

    public Hand? ActiveHand { get; private set; }

    /// <summary>Vrai du règlement d'une manche jusqu'à la donne suivante : le bilan des sièges est lisible.</summary>
    public bool ResultsAvailable { get; private set; }

    public IReadOnlyList<DrinkTransfer> Transfers => _transfers;

    public int ShoeRemaining => _shoe.Remaining;

    public int ShoeTotal => _shoe.Total;

    // ------------------------------------------------------------------ Joueurs

    public int MaxPlayers => Rules.MaxPlayers;

    public IReadOnlyList<TablePlayer> Players => _seats.Select(s => new TablePlayer(s.PlayerId, s.Name)).ToList();

    void ITableGame.AddPlayer(Guid playerId, string name) => AddPlayer(playerId, name);

    public Seat AddPlayer(Guid playerId, string name)
    {
        if (_seats.Count >= Rules.MaxPlayers)
        {
            throw new GameRuleException("table_full", "La table est complète.");
        }

        var seat = new Seat(playerId, name);
        _seats.Add(seat);
        return seat;
    }

    public void RemovePlayer(Guid playerId, DateTimeOffset now)
    {
        var seat = GetSeat(playerId);
        var wasActive = ReferenceEquals(seat, ActiveSeat);

        _shoe.Discard(seat.Hands.SelectMany(h => h.Cards));
        _seats.Remove(seat);

        if (_seats.Count == 0)
        {
            ResetTable();
            return;
        }

        switch (Phase)
        {
            case GamePhase.Betting:
                TryCloseBetting(now, deadlineReached: false);
                break;
            case GamePhase.PlayerTurns when wasActive:
                MoveToNextHand(now);
                break;
            case GamePhase.Distribution:
                if (_seats.Count == 1)
                {
                    // Le dernier joueur restant n'a plus personne à qui donner.
                    _seats[0].DistributionDone = true;
                }

                if (_seats.All(s => s.DistributionDone))
                {
                    EnterResults(now);
                }

                break;
        }
    }

    public void SetConnected(Guid playerId, bool isConnected, DateTimeOffset now)
    {
        GetSeat(playerId).IsConnected = isConnected;

        // Un joueur qui se déconnecte ne doit pas bloquer la donne des autres.
        if (Phase == GamePhase.Betting)
        {
            TryCloseBetting(now, deadlineReached: false);
        }
    }

    // ------------------------------------------------------------------ Mises

    /// <summary>Pose ou modifie la mise du joueur : des gorgées, un cul sec, ou rien pour passer la manche.</summary>
    public void PlaceBet(Guid playerId, Bet bet, DateTimeOffset now)
    {
        EnsurePhase(GamePhase.Betting);
        var seat = GetSeat(playerId);
        EnsureValidBet(bet);

        seat.Bet = bet.Amount;
        seat.BetUnit = bet.Unit;

        if (!bet.IsPass)
        {
            Deadline ??= now + _timings.Betting;
        }

        TryCloseBetting(now, deadlineReached: false);
    }

    private void EnsureValidBet(Bet bet)
    {
        if (bet.Unit == DrinkUnit.CulSec)
        {
            if (!Rules.AllowCulSec)
            {
                throw new GameRuleException("cul_sec_not_allowed", "Le cul sec n'est pas autorisé à cette table.");
            }

            // On mise un seul cul sec à la fois ; seul « doubler » peut en mettre deux en jeu.
            if (bet.Amount != 1)
            {
                throw new GameRuleException("invalid_bet", "On ne peut miser qu'un cul sec à la fois.");
            }

            return;
        }

        if (!bet.IsPass && (bet.Amount < Rules.MinBet || bet.Amount > Rules.MaxBet))
        {
            throw new GameRuleException(
                "invalid_bet",
                $"La mise doit être comprise entre {Rules.MinBet} et {Rules.MaxBet} gorgées, ou 0 pour passer.");
        }
    }

    private void TryCloseBetting(DateTimeOffset now, bool deadlineReached)
    {
        if (!_seats.Any(s => s.Bet > 0))
        {
            // Personne ne joue : on attend sans minuteur qu'une mise arrive.
            Deadline = null;
            return;
        }

        var everyoneAnswered = _seats.Where(s => s.IsConnected).All(s => s.Bet is not null);
        if (deadlineReached || everyoneAnswered)
        {
            Deal(now);
        }
    }

    private void Deal(DateTimeOffset now)
    {
        RoundNumber++;
        ResultsAvailable = false;
        _transfers.Clear();
        DealerHoleRevealed = false;

        foreach (var seat in _seats)
        {
            seat.ResetRoundOutcome();
            seat.Bet ??= 0;
            seat.InRound = seat.Bet > 0;
            if (seat.InRound)
            {
                seat.MutableHands.Add(new Hand(seat.Bet.Value));
            }
        }

        var playing = _seats.Where(s => s.InRound).ToList();
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var seat in playing)
            {
                seat.Hands[0].Add(_shoe.Draw());
            }

            _dealerCards.Add(_shoe.Draw());
        }

        foreach (var hand in playing.Select(s => s.Hands[0]).Where(h => HandEvaluator.IsNatural(h.Cards)))
        {
            hand.MarkBlackjack();
        }

        // Le croupier regarde sa carte cachée : s'il a blackjack, personne ne joue sa main.
        if (HandEvaluator.IsNatural(_dealerCards))
        {
            StartDealerTurn(now);
        }
        else
        {
            MoveToNextHand(now);
        }
    }

    // ------------------------------------------------------------------ Tours des joueurs

    public IReadOnlyList<PlayerAction> GetAllowedActions(Guid playerId)
    {
        if (Phase != GamePhase.PlayerTurns || ActiveSeat?.PlayerId != playerId || ActiveHand is null)
        {
            return [];
        }

        var actions = new List<PlayerAction> { PlayerAction.Hit, PlayerAction.Stand };
        if (ActiveHand.CanDouble)
        {
            actions.Add(PlayerAction.Double);
        }

        if (ActiveHand.IsPair && ActiveSeat.Hands.Count < Rules.MaxHandsPerPlayer)
        {
            actions.Add(PlayerAction.Split);
        }

        return actions;
    }

    public void Act(Guid playerId, PlayerAction action, DateTimeOffset now)
    {
        EnsurePhase(GamePhase.PlayerTurns);
        if (ActiveSeat?.PlayerId != playerId || ActiveHand is null)
        {
            throw new GameRuleException("not_your_turn", "Ce n'est pas ton tour.");
        }

        if (!GetAllowedActions(playerId).Contains(action))
        {
            throw new GameRuleException("action_not_allowed", "Cette action n'est pas possible sur cette main.");
        }

        var hand = ActiveHand;
        switch (action)
        {
            case PlayerAction.Hit:
                hand.Add(_shoe.Draw());
                if (hand.Value.IsBust)
                {
                    hand.MarkBust();
                }
                else if (hand.Value.Total == 21)
                {
                    hand.Stand();
                }
                else
                {
                    // La main continue : le joueur a de nouveau tout son temps.
                    Deadline = now + _timings.Turn;
                    return;
                }

                break;

            case PlayerAction.Stand:
                hand.Stand();
                break;

            case PlayerAction.Double:
                hand.Double();
                hand.Add(_shoe.Draw());
                if (hand.Value.IsBust)
                {
                    hand.MarkBust();
                }

                break;

            case PlayerAction.Split:
                Split(ActiveSeat, hand);
                break;
        }

        MoveToNextHand(now);
    }

    private void Split(Seat seat, Hand hand)
    {
        var splitAces = hand.Cards[0].Rank == Rank.Ace;
        var index = seat.MutableHands.IndexOf(hand);
        var halves = hand.Cards.Select(card =>
        {
            var half = new Hand(hand.Bet, isFromSplit: true);
            half.Add(card);
            half.Add(_shoe.Draw());
            return half;
        }).ToList();

        seat.MutableHands.RemoveAt(index);
        seat.MutableHands.InsertRange(index, halves);

        foreach (var half in halves)
        {
            // As séparés : une seule carte par as. Et un 21 n'a plus rien à tirer.
            if (splitAces || half.Value.Total == 21)
            {
                half.Stand();
            }
        }
    }

    /// <summary>Donne la main à la prochaine main encore à jouer, sinon passe au croupier.</summary>
    private void MoveToNextHand(DateTimeOffset now)
    {
        foreach (var seat in _seats.Where(s => s.InRound))
        {
            var hand = seat.Hands.FirstOrDefault(h => h.Status == HandStatus.Playing);
            if (hand is null)
            {
                continue;
            }

            Phase = GamePhase.PlayerTurns;
            ActiveSeat = seat;
            ActiveHand = hand;
            Deadline = now + _timings.Turn;
            return;
        }

        StartDealerTurn(now);
    }

    // ------------------------------------------------------------------ Croupier

    private void StartDealerTurn(DateTimeOffset now)
    {
        Phase = GamePhase.DealerTurn;
        ActiveSeat = null;
        ActiveHand = null;
        Deadline = now + _timings.DealerStep;
    }

    /// <summary>Un geste du croupier par pas : retourner sa carte, tirer une carte, puis régler.</summary>
    private void DealerStep(DateTimeOffset now)
    {
        if (!DealerHoleRevealed)
        {
            DealerHoleRevealed = true;
        }
        else if (DealerMustDraw())
        {
            _dealerCards.Add(_shoe.Draw());
        }
        else
        {
            Settle(now);
            return;
        }

        Deadline = now + _timings.DealerStep;
    }

    private bool DealerMustDraw()
    {
        // Inutile de tirer si aucune main n'attend d'être comparée.
        if (!_seats.SelectMany(s => s.Hands).Any(h => h.IsStanding))
        {
            return false;
        }

        var value = HandEvaluator.Evaluate(_dealerCards);
        return value.Total < 17 || (value.Total == 17 && value.IsSoft && Rules.DealerHitsSoft17);
    }

    // ------------------------------------------------------------------ Règlement et gorgées

    private void Settle(DateTimeOffset now)
    {
        var dealer = HandEvaluator.Evaluate(_dealerCards);
        var dealerBlackjack = HandEvaluator.IsNatural(_dealerCards);

        foreach (var seat in _seats)
        {
            // Toutes les mains d'un joueur partagent l'unité de sa mise (gorgées ou culs secs).
            seat.ResultUnit = seat.BetUnit;

            foreach (var hand in seat.Hands)
            {
                var outcome = ResolveOutcome(hand, dealer, dealerBlackjack);
                hand.Settle(outcome);

                switch (outcome)
                {
                    case HandOutcome.Lose:
                        seat.Lost += hand.Bet;
                        break;
                    case HandOutcome.Win:
                        seat.ToDistribute += hand.Bet;
                        break;
                    case HandOutcome.Blackjack:
                        seat.ToDistribute += Rules.BlackjackPayout(hand.Bet);
                        break;
                }
            }
        }

        ResultsAvailable = true;

        var canDistribute = _seats.Count > 1;
        foreach (var seat in _seats)
        {
            seat.DistributionDone = !(canDistribute && seat.ToDistribute > 0);
        }

        if (_seats.All(s => s.DistributionDone))
        {
            EnterResults(now);
        }
        else
        {
            Phase = GamePhase.Distribution;
            Deadline = now + _timings.Distribution;
        }
    }

    private static HandOutcome ResolveOutcome(Hand hand, HandValue dealer, bool dealerBlackjack)
    {
        if (hand.Status == HandStatus.Bust)
        {
            return HandOutcome.Lose;
        }

        if (hand.Status == HandStatus.Blackjack)
        {
            return dealerBlackjack ? HandOutcome.Push : HandOutcome.Blackjack;
        }

        if (dealerBlackjack)
        {
            return HandOutcome.Lose;
        }

        if (dealer.IsBust)
        {
            return HandOutcome.Win;
        }

        var total = hand.Value.Total;
        return total > dealer.Total ? HandOutcome.Win
            : total < dealer.Total ? HandOutcome.Lose
            : HandOutcome.Push;
    }

    /// <summary>
    /// Le gagnant répartit tout ce qu'il a gagné entre les autres joueurs de la table.
    /// Les quantités sont dans l'unité de sa mise (gorgées ou culs secs).
    /// </summary>
    public void Distribute(Guid playerId, IReadOnlyDictionary<Guid, int> allocations, DateTimeOffset now)
    {
        EnsurePhase(GamePhase.Distribution);
        var seat = GetSeat(playerId);

        if (seat.DistributionDone)
        {
            throw new GameRuleException("nothing_to_distribute", "Tu n'as plus rien à distribuer.");
        }

        if (allocations.Values.Any(amount => amount <= 0) || allocations.Values.Sum() != seat.ToDistribute)
        {
            var what = seat.ResultUnit == DrinkUnit.CulSec ? "cul(s) sec(s)" : "gorgée(s)";
            throw new GameRuleException(
                "invalid_distribution",
                $"Tu dois distribuer exactement {seat.ToDistribute} {what}.");
        }

        if (allocations.Keys.Any(id => id == playerId || _seats.All(s => s.PlayerId != id)))
        {
            throw new GameRuleException("invalid_distribution", "Tu ne peux donner qu'aux autres joueurs de la table.");
        }

        foreach (var (targetId, sips) in allocations)
        {
            AddTransfer(seat, GetSeat(targetId), sips);
        }

        seat.DistributionDone = true;

        if (_seats.All(s => s.DistributionDone))
        {
            EnterResults(now);
        }
    }

    /// <summary>Répartit au hasard, gorgée par gorgée, pour les gagnants qui n'ont pas répondu à temps.</summary>
    private void DistributeForAbsentWinners()
    {
        foreach (var seat in _seats.Where(s => !s.DistributionDone))
        {
            var others = _seats.Where(s => !ReferenceEquals(s, seat)).ToList();
            var shares = new int[others.Count];
            var start = _random.Next(others.Count);

            for (var i = 0; i < seat.ToDistribute; i++)
            {
                shares[(start + i) % others.Count]++;
            }

            for (var i = 0; i < others.Count; i++)
            {
                if (shares[i] > 0)
                {
                    AddTransfer(seat, others[i], shares[i]);
                }
            }

            seat.DistributionDone = true;
        }
    }

    private void AddTransfer(Seat from, Seat to, int amount) =>
        _transfers.Add(new DrinkTransfer(from.PlayerId, from.Name, to.PlayerId, to.Name, amount, from.ResultUnit));

    private void EnterResults(DateTimeOffset now)
    {
        Phase = GamePhase.Results;
        Deadline = now + _timings.Results;

        foreach (var seat in _seats)
        {
            var totals = seat.Totals.AddDrunk(seat.Lost, seat.ResultUnit);

            foreach (var received in _transfers.Where(t => t.ToPlayerId == seat.PlayerId))
            {
                totals = totals.AddDrunk(received.Amount, received.Unit);
            }

            foreach (var given in _transfers.Where(t => t.FromPlayerId == seat.PlayerId))
            {
                totals = totals.AddGiven(given.Amount, given.Unit);
            }

            seat.Totals = totals;
        }

        _completedRound = new RoundSummary(
            RoundNumber,
            _dealerCards.ToList(),
            HandEvaluator.Evaluate(_dealerCards).Total,
            _seats.SelectMany(s => s.Hands.Select(h => new RoundHandSummary(
                s.PlayerId, h.Cards.ToList(), h.Value.Total, h.Bet, s.ResultUnit, h.Outcome!.Value))).ToList(),
            _transfers.ToList(),
            _seats.Select(s => new PlayerTotals(s.PlayerId, s.Totals)).ToList());
    }

    /// <summary>Renvoie la manche qui vient de se terminer, une seule fois, pour l'enregistrer dans l'historique.</summary>
    public RoundSummary? TakeCompletedRound()
    {
        var round = _completedRound;
        _completedRound = null;
        return round;
    }

    // ------------------------------------------------------------------ Horloge

    /// <summary>Fait avancer la phase en cours si son échéance est passée. Renvoie vrai si l'état a changé.</summary>
    public bool Tick(DateTimeOffset now)
    {
        if (Deadline is null || now < Deadline)
        {
            return false;
        }

        switch (Phase)
        {
            case GamePhase.Betting:
                TryCloseBetting(now, deadlineReached: true);
                break;
            case GamePhase.PlayerTurns:
                ActiveHand!.Stand();
                MoveToNextHand(now);
                break;
            case GamePhase.DealerTurn:
                DealerStep(now);
                break;
            case GamePhase.Distribution:
                DistributeForAbsentWinners();
                EnterResults(now);
                break;
            case GamePhase.Results:
                StartBetting();
                break;
        }

        return true;
    }

    /// <summary>Ramasse les cartes et rouvre les mises. Le bilan reste lisible jusqu'à la donne suivante.</summary>
    private void StartBetting()
    {
        CollectCards();
        Phase = GamePhase.Betting;
        Deadline = null;
    }

    private void ResetTable()
    {
        CollectCards();
        _transfers.Clear();
        ResultsAvailable = false;
        Phase = GamePhase.Betting;
        Deadline = null;
    }

    private void CollectCards()
    {
        _shoe.Discard(_dealerCards);
        _dealerCards.Clear();
        DealerHoleRevealed = false;
        ActiveSeat = null;
        ActiveHand = null;

        foreach (var seat in _seats)
        {
            _shoe.Discard(seat.Hands.SelectMany(h => h.Cards));
            seat.MutableHands.Clear();
            seat.Bet = null;
            seat.BetUnit = DrinkUnit.Sip;
            seat.InRound = false;
        }
    }

    // ------------------------------------------------------------------ Utilitaires

    private Seat GetSeat(Guid playerId) =>
        _seats.FirstOrDefault(s => s.PlayerId == playerId)
        ?? throw new GameRuleException("unknown_player", "Ce joueur n'est pas assis à cette table.");

    private void EnsurePhase(GamePhase expected)
    {
        if (Phase != expected)
        {
            throw new GameRuleException("wrong_phase", "Cette action n'est pas possible à ce moment de la manche.");
        }
    }
}
