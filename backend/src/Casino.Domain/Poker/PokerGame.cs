using Casino.Domain.Blackjack;
using Casino.Domain.Cards;
using Casino.Domain.Common;

namespace Casino.Domain.Poker;

/// <summary>Un joueur assis à la table de poker et son état pour la main en cours.</summary>
public sealed class PokerSeat
{
    private readonly List<Card> _holeCards = [];

    internal PokerSeat(Guid playerId, string name)
    {
        PlayerId = playerId;
        Name = name;
    }

    public Guid PlayerId { get; }

    public string Name { get; }

    public bool IsConnected { get; internal set; }

    /// <summary>Vrai si le joueur a reçu des cartes pour la main en cours.</summary>
    public bool InHand { get; internal set; }

    public bool Folded { get; internal set; }

    /// <summary>Encore en course pour le pot.</summary>
    public bool IsLive => InHand && !Folded;

    public IReadOnlyList<Card> HoleCards => _holeCards;

    /// <summary>Gorgées misées pendant le tour d'enchères en cours.</summary>
    public int StreetBet { get; internal set; }

    /// <summary>Gorgées misées depuis le début de la main.</summary>
    public int TotalBet { get; internal set; }

    internal bool HasActed { get; set; }

    public bool IsWinner { get; internal set; }

    /// <summary>Nom de sa meilleure main, connu seulement à l'abattage.</summary>
    public string? HandName { get; internal set; }

    /// <summary>Part du pot que le gagnant doit distribuer aux autres.</summary>
    public int ToDistribute { get; internal set; }

    public bool DistributionDone { get; internal set; }

    /// <summary>Cumuls depuis l'arrivée à la table.</summary>
    public DrinkTotals Totals { get; internal set; }

    internal List<Card> MutableHoleCards => _holeCards;

    internal void ResetForHand()
    {
        _holeCards.Clear();
        InHand = false;
        Folded = false;
        StreetBet = 0;
        TotalBet = 0;
        HasActed = false;
        IsWinner = false;
        HandName = null;
        ToDistribute = 0;
        DistributionDone = false;
    }
}

/// <summary>
/// Moteur d'une table de Texas Hold'em à gorgées : blindes, quatre tours d'enchères limitées,
/// abattage, puis le gagnant distribue le pot — personne ne boit sa propre mise.
/// <para>
/// Comme le moteur de blackjack, la classe est pure : l'appelant fournit l'instant courant et
/// appelle <see cref="Tick"/> régulièrement. Elle n'est pas thread-safe.
/// </para>
/// </summary>
public sealed class PokerGame : ITableGame
{
    private readonly PokerTimings _timings;
    private readonly Func<IShoe> _newDeck;
    private readonly IRandomSource _random;
    private readonly List<PokerSeat> _seats = [];
    private readonly List<Card> _board = [];
    private readonly List<DrinkTransfer> _transfers = [];
    private IShoe? _deck;
    private int _raisesThisStreet;
    /// <summary>Mises des joueurs partis en cours de main : elles restent dans le pot.</summary>
    private int _deadSips;
    private RoundSummary? _completedRound;

    /// <param name="newDeck">Fournit un jeu de 52 cartes fraîchement mélangé pour chaque main.</param>
    public PokerGame(PokerRules rules, PokerTimings timings, Func<IShoe> newDeck, IRandomSource random)
    {
        Rules = rules;
        _timings = timings;
        _newDeck = newDeck;
        _random = random;
    }

    public PokerRules Rules { get; }

    public PokerPhase Phase { get; private set; } = PokerPhase.Waiting;

    /// <summary>Échéance de la phase en cours ; null quand rien n'est minuté.</summary>
    public DateTimeOffset? Deadline { get; private set; }

    public int HandNumber { get; private set; }

    public IReadOnlyList<PokerSeat> Seats => _seats;

    /// <summary>Cartes communes posées sur la table.</summary>
    public IReadOnlyList<Card> Board => _board;

    public int Pot => _deadSips + _seats.Sum(s => s.TotalBet);

    /// <summary>Enchère à égaler pendant le tour en cours.</summary>
    public int CurrentBet { get; private set; }

    public PokerSeat? ActiveSeat { get; private set; }

    public PokerSeat? ButtonSeat { get; private set; }

    public PokerSeat? SmallBlindSeat { get; private set; }

    public PokerSeat? BigBlindSeat { get; private set; }

    /// <summary>Vrai si la main est allée à l'abattage : les cartes des joueurs encore en course sont visibles.</summary>
    public bool Showdown { get; private set; }

    /// <summary>Vrai du règlement d'une main jusqu'à la donne suivante.</summary>
    public bool ResultsAvailable { get; private set; }

    public IReadOnlyList<DrinkTransfer> Transfers => _transfers;

    public bool IsBettingPhase => Phase is PokerPhase.PreFlop or PokerPhase.Flop or PokerPhase.Turn or PokerPhase.River;

    // ------------------------------------------------------------------ Joueurs

    public int MaxPlayers => Rules.MaxPlayers;

    public IReadOnlyList<TablePlayer> Players => _seats.Select(s => new TablePlayer(s.PlayerId, s.Name)).ToList();

    public void AddPlayer(Guid playerId, string name)
    {
        if (_seats.Count >= Rules.MaxPlayers)
        {
            throw new GameRuleException("table_full", "La table est complète.");
        }

        _seats.Add(new PokerSeat(playerId, name));
    }

    public void RemovePlayer(Guid playerId, DateTimeOffset now)
    {
        var seat = GetSeat(playerId);

        if (IsBettingPhase && seat.IsLive)
        {
            // Partir en cours de main, c'est se coucher : sa mise reste dans le pot.
            var wasActive = ReferenceEquals(seat, ActiveSeat);
            seat.Folded = true;
            if (wasActive)
            {
                AfterAction(seat, now);
            }
            else
            {
                CloseRoundIfComplete(now);
            }
        }

        if (ReferenceEquals(seat, ButtonSeat))
        {
            // Le bouton recule d'un cran : la rotation suivante retombera sur le bon joueur.
            ButtonSeat = _seats.Count > 1 ? PreviousSeat(seat) : null;
        }

        _deadSips += seat.TotalBet;
        _seats.Remove(seat);

        if (_seats.Count == 0)
        {
            ResetTable();
            return;
        }

        switch (Phase)
        {
            case PokerPhase.Waiting:
                UpdateStart(now);
                break;
            case PokerPhase.Distribution:
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
        UpdateStart(now);
    }

    // ------------------------------------------------------------------ Donne

    /// <summary>Entre deux mains : lance le compte à rebours de la donne dès que deux joueurs sont là.</summary>
    private void UpdateStart(DateTimeOffset now)
    {
        if (Phase != PokerPhase.Waiting)
        {
            return;
        }

        if (_seats.Count(s => s.IsConnected) >= 2)
        {
            Deadline ??= now + _timings.StartDelay;
        }
        else
        {
            Deadline = null;
        }
    }

    private void Deal(DateTimeOffset now)
    {
        HandNumber++;
        ResultsAvailable = false;
        Showdown = false;
        _transfers.Clear();
        _board.Clear();
        _deadSips = 0;
        _deck = _newDeck();

        foreach (var seat in _seats)
        {
            seat.ResetForHand();
            // Un joueur déconnecté ne reçoit pas de cartes : il ne bloquera pas la main.
            seat.InHand = seat.IsConnected;
        }

        var players = _seats.Where(s => s.InHand).ToList();
        ButtonSeat = NextSeat(ButtonSeat, s => s.InHand)!;

        // En tête-à-tête, le bouton paie la petite blinde et parle en premier avant le flop.
        SmallBlindSeat = players.Count == 2 ? ButtonSeat : NextSeat(ButtonSeat, s => s.InHand)!;
        BigBlindSeat = NextSeat(SmallBlindSeat, s => s.InHand)!;
        Pay(SmallBlindSeat, Rules.SmallBlind);
        Pay(BigBlindSeat, Rules.BigBlind);
        CurrentBet = Rules.BigBlind;
        _raisesThisStreet = 0;

        for (var pass = 0; pass < 2; pass++)
        {
            var seat = SmallBlindSeat;
            do
            {
                seat.MutableHoleCards.Add(_deck.Draw());
                seat = NextSeat(seat, s => s.InHand)!;
            }
            while (!ReferenceEquals(seat, SmallBlindSeat));
        }

        Phase = PokerPhase.PreFlop;
        SetActive(NextSeat(BigBlindSeat, s => s.IsLive)!, now);
    }

    // ------------------------------------------------------------------ Enchères

    /// <summary>Gorgées que le joueur doit ajouter pour suivre.</summary>
    public int AmountToCall(PokerSeat seat) => CurrentBet - seat.StreetBet;

    public bool CanRaise => _raisesThisStreet < Rules.MaxRaisesPerStreet;

    public IReadOnlyList<PokerAction> GetAllowedActions(Guid playerId)
    {
        if (!IsBettingPhase || ActiveSeat?.PlayerId != playerId)
        {
            return [];
        }

        var actions = new List<PokerAction>
        {
            PokerAction.Fold,
            AmountToCall(ActiveSeat) == 0 ? PokerAction.Check : PokerAction.Call,
        };
        if (CanRaise)
        {
            actions.Add(PokerAction.Raise);
        }

        return actions;
    }

    /// <param name="raiseBy">Pour une relance : gorgées ajoutées à l'enchère en cours. Ignoré sinon.</param>
    public void Act(Guid playerId, PokerAction action, int raiseBy, DateTimeOffset now)
    {
        if (!IsBettingPhase)
        {
            throw new GameRuleException("wrong_phase", "Cette action n'est pas possible à ce moment de la main.");
        }

        if (ActiveSeat?.PlayerId != playerId)
        {
            throw new GameRuleException("not_your_turn", "Ce n'est pas ton tour.");
        }

        if (!GetAllowedActions(playerId).Contains(action))
        {
            throw new GameRuleException("action_not_allowed", "Cette action n'est pas possible maintenant.");
        }

        var seat = ActiveSeat;
        switch (action)
        {
            case PokerAction.Fold:
                seat.Folded = true;
                break;

            case PokerAction.Call:
                Pay(seat, AmountToCall(seat));
                break;

            case PokerAction.Raise:
                if (raiseBy < Rules.MinRaise || raiseBy > Rules.MaxRaise)
                {
                    throw new GameRuleException(
                        "invalid_raise",
                        $"La relance doit être comprise entre {Rules.MinRaise} et {Rules.MaxRaise} gorgées.");
                }

                CurrentBet += raiseBy;
                Pay(seat, AmountToCall(seat));
                _raisesThisStreet++;

                // Une relance rouvre la parole à tous les autres.
                foreach (var other in _seats.Where(s => s.IsLive))
                {
                    other.HasActed = false;
                }

                break;
        }

        seat.HasActed = true;
        AfterAction(seat, now);
    }

    private void AfterAction(PokerSeat actor, DateTimeOffset now)
    {
        if (!CloseRoundIfComplete(now))
        {
            SetActive(NextSeat(actor, NeedsToAct)!, now);
        }
    }

    /// <summary>Règle la main s'il ne reste qu'un joueur, ou passe au tour suivant si tout le monde a suivi.</summary>
    private bool CloseRoundIfComplete(DateTimeOffset now)
    {
        var live = _seats.Where(s => s.IsLive).ToList();
        if (live.Count <= 1)
        {
            Settle(now, showdown: false);
            return true;
        }

        if (live.Any(NeedsToAct))
        {
            return false;
        }

        NextStreet(now);
        return true;
    }

    private bool NeedsToAct(PokerSeat seat) =>
        seat.IsLive && !(seat.HasActed && seat.StreetBet == CurrentBet);

    private void NextStreet(DateTimeOffset now)
    {
        if (Phase == PokerPhase.River)
        {
            Settle(now, showdown: true);
            return;
        }

        foreach (var seat in _seats)
        {
            seat.StreetBet = 0;
            seat.HasActed = false;
        }

        CurrentBet = 0;
        _raisesThisStreet = 0;

        var cards = Phase == PokerPhase.PreFlop ? 3 : 1;
        for (var i = 0; i < cards; i++)
        {
            _board.Add(_deck!.Draw());
        }

        Phase++;
        SetActive(NextSeat(ButtonSeat, s => s.IsLive)!, now);
    }

    private void SetActive(PokerSeat seat, DateTimeOffset now)
    {
        ActiveSeat = seat;
        Deadline = now + _timings.Turn;
    }

    private static void Pay(PokerSeat seat, int sips)
    {
        seat.StreetBet += sips;
        seat.TotalBet += sips;
    }

    // ------------------------------------------------------------------ Règlement et gorgées

    private void Settle(DateTimeOffset now, bool showdown)
    {
        var live = _seats.Where(s => s.IsLive).ToList();
        var winners = live;

        if (showdown)
        {
            var values = live.ToDictionary(s => s, s => PokerHandEvaluator.Evaluate([.. s.HoleCards, .. _board]));
            foreach (var (seat, value) in values)
            {
                seat.HandName = value.Name;
            }

            var best = values.Values.Max();
            winners = live.Where(s => values[s].CompareTo(best) == 0).ToList();
        }

        // Pot partagé entre gagnants à égalité ; les gorgées indivisibles vont aux premiers servis.
        var pot = Pot;
        for (var i = 0; i < winners.Count; i++)
        {
            winners[i].IsWinner = true;
            winners[i].ToDistribute = pot / winners.Count + (i < pot % winners.Count ? 1 : 0);
        }

        Showdown = showdown;
        ResultsAvailable = true;
        ActiveSeat = null;

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
            Phase = PokerPhase.Distribution;
            Deadline = now + _timings.Distribution;
        }
    }

    /// <summary>Le gagnant répartit sa part du pot entre les autres joueurs de la table.</summary>
    public void Distribute(Guid playerId, IReadOnlyDictionary<Guid, int> allocations, DateTimeOffset now)
    {
        if (Phase != PokerPhase.Distribution)
        {
            throw new GameRuleException("wrong_phase", "Cette action n'est pas possible à ce moment de la main.");
        }

        var seat = GetSeat(playerId);
        if (seat.DistributionDone)
        {
            throw new GameRuleException("nothing_to_distribute", "Tu n'as plus rien à distribuer.");
        }

        if (allocations.Values.Any(amount => amount <= 0) || allocations.Values.Sum() != seat.ToDistribute)
        {
            throw new GameRuleException(
                "invalid_distribution",
                $"Tu dois distribuer exactement {seat.ToDistribute} gorgée(s).");
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
            if (others.Count > 0)
            {
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
            }

            seat.DistributionDone = true;
        }
    }

    private void AddTransfer(PokerSeat from, PokerSeat to, int sips) =>
        _transfers.Add(new DrinkTransfer(from.PlayerId, from.Name, to.PlayerId, to.Name, sips, DrinkUnit.Sip));

    private void EnterResults(DateTimeOffset now)
    {
        Phase = PokerPhase.Results;
        Deadline = now + _timings.Results;

        foreach (var seat in _seats)
        {
            var received = _transfers.Where(t => t.ToPlayerId == seat.PlayerId).Sum(t => t.Amount);
            var given = _transfers.Where(t => t.FromPlayerId == seat.PlayerId).Sum(t => t.Amount);
            seat.Totals = seat.Totals.AddDrunk(received, DrinkUnit.Sip).AddGiven(given, DrinkUnit.Sip);
        }

        // Historique : le tableau tient lieu de « cartes du croupier », le pot de total.
        _completedRound = new RoundSummary(
            HandNumber,
            _board.ToList(),
            Pot,
            _seats.Where(s => s.InHand).Select(s => new RoundHandSummary(
                s.PlayerId,
                s.HoleCards.ToList(),
                Total: 0,
                s.TotalBet,
                DrinkUnit.Sip,
                s.IsWinner ? HandOutcome.Win : HandOutcome.Lose)).ToList(),
            _transfers.ToList(),
            _seats.Select(s => new PlayerTotals(s.PlayerId, s.Totals)).ToList());
    }

    public RoundSummary? TakeCompletedRound()
    {
        var round = _completedRound;
        _completedRound = null;
        return round;
    }

    // ------------------------------------------------------------------ Horloge

    public bool Tick(DateTimeOffset now)
    {
        if (Deadline is null || now < Deadline)
        {
            return false;
        }

        switch (Phase)
        {
            case PokerPhase.Waiting:
                Deadline = null;
                if (_seats.Count(s => s.IsConnected) >= 2)
                {
                    Deal(now);
                }

                break;

            case PokerPhase.Distribution:
                DistributeForAbsentWinners();
                EnterResults(now);
                break;

            case PokerPhase.Results:
                Phase = PokerPhase.Waiting;
                Deadline = null;
                UpdateStart(now);
                break;

            default:
                // Temps écoulé : le joueur checke s'il le peut, sinon il se couche.
                var seat = ActiveSeat!;
                var action = AmountToCall(seat) == 0 ? PokerAction.Check : PokerAction.Fold;
                Act(seat.PlayerId, action, raiseBy: 0, now);
                break;
        }

        return true;
    }

    private void ResetTable()
    {
        _board.Clear();
        _transfers.Clear();
        _deadSips = 0;
        ActiveSeat = null;
        ButtonSeat = null;
        SmallBlindSeat = null;
        BigBlindSeat = null;
        ResultsAvailable = false;
        Showdown = false;
        Phase = PokerPhase.Waiting;
        Deadline = null;
    }

    // ------------------------------------------------------------------ Utilitaires

    /// <summary>Premier siège après <paramref name="from"/>, dans le sens du jeu, qui vérifie la condition.</summary>
    private PokerSeat? NextSeat(PokerSeat? from, Func<PokerSeat, bool> predicate)
    {
        var start = from is null ? -1 : _seats.IndexOf(from);
        for (var offset = 1; offset <= _seats.Count; offset++)
        {
            var seat = _seats[(start + offset + _seats.Count) % _seats.Count];
            if (predicate(seat))
            {
                return seat;
            }
        }

        return null;
    }

    private PokerSeat PreviousSeat(PokerSeat seat) =>
        _seats[(_seats.IndexOf(seat) - 1 + _seats.Count) % _seats.Count];

    private PokerSeat GetSeat(Guid playerId) =>
        _seats.FirstOrDefault(s => s.PlayerId == playerId)
        ?? throw new GameRuleException("unknown_player", "Ce joueur n'est pas assis à cette table.");
}
