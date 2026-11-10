using _7WondersGame.src.models.Wonders;
using Serilog;

namespace _7WondersGame.src.models.Players
{
    public abstract class Player
    {
        public int Id { get; set; }

        public Wonder Board { get; set; }
        public List<Card> HandCards { get; set; }
        public List<Card> PlayedCards { get; set; }
        public List<Card> PlayableCards { get; set; }

        public int VictoryTokens = 0;
        public int DefeatTokens = 0;
        public int VictoryPoints = 0;
        // Babylon B stage 2
        public bool PlaySeventh { get; set; } = false;
        // Olympia B stage 1
        public bool WonderRawCheap = false;
        public bool RawCheapEast = false;
        public bool RawCheapWest = false;
        // Marketplace card effect
        public bool ManufCheap = false;
        // Olympia A stage 2
        public bool FreeCardOnce = false;
        // Halikarnassos A stage 2 & B stage 1-3
        public bool DiscardFree = false;
        // Science guild effect
        public bool ExtraScience = false;
        public bool CanBuildWonder = false;

        public Dictionary<Resource, int> Resources = new Dictionary<Resource, int>()
        {
            { Resource.Wood, 0 },
            { Resource.Stone, 0 },
            { Resource.Ore, 0 },
            { Resource.Clay, 0 },
            { Resource.Glass, 0 },
            { Resource.Loom, 0 },
            { Resource.Papyrus, 0 },
            { Resource.Gear, 0 },
            { Resource.Compass, 0 },
            { Resource.Tablet, 0 },
            { Resource.Coins, 3 },
            { Resource.Shields, 0 },
        };

        public Player PlayerEast { get; set; }
        public Player PlayerWest { get; set; }

        /// <summary>
        /// The trace logger of the game this player is in (set by <see cref="Game.Loop"/>).
        /// Silent by default, and copies never get it, so MCTS rollouts stay silent.
        /// </summary>
        public ILogger Trace { get; set; } = Serilog.Core.Logger.None;

        public Player(int id)
        {
            Id = id;
            HandCards = new List<Card>();
            PlayedCards = new List<Card>();
            PlayableCards = new List<Card>();
        }

        /// <summary>
        /// Copies all player data. Neighboring players must be set seperatly!
        /// </summary>
        /// <returns></returns>
        public abstract object DeepCopy();

        /// <summary>
        /// The agent's settings as JSON, logged with every result row (B2).
        /// By default these are the simple-valued instance fields declared by the agent class itself
        /// (for example an MCTS agent's exploration constant and iteration count), sorted by name.
        /// </summary>
        public virtual string ConfigJson()
        {
            var config = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (name, field) in SettingFields(GetType()))
                config.Add(name, field.FieldType.IsEnum ? field.GetValue(this)?.ToString() : field.GetValue(this));
            return System.Text.Json.JsonSerializer.Serialize(config);
        }

        /// <summary>
        /// An agent's settings: the instance fields its own classes (below Player) declare with a primitive, enum,
        /// string or decimal type. They are what <see cref="ConfigJson"/> writes and what the command line can set.
        /// Auto-property backing fields are named after the property.
        /// </summary>
        public static IEnumerable<(string Name, System.Reflection.FieldInfo Field)> SettingFields(Type agentType)
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (Type? t = agentType; t != null && t != typeof(Player); t = t.BaseType)
            {
                foreach (var field in t.GetFields(flags))
                {
                    Type ft = field.FieldType;
                    if (!(ft.IsPrimitive || ft.IsEnum || ft == typeof(string) || ft == typeof(decimal)))
                        continue;
                    // auto-property backing fields are named "<Name>k__BackingField"
                    string name = field.Name.StartsWith("<") ? field.Name[1..field.Name.IndexOf('>')] : field.Name;
                    if (seen.Add(name))
                        yield return (name, field);
                }
            }
        }


        // =========== Build related functions ===========
        // =========== Wonder related ===========
        /// <summary>Sets and returns <see cref="CanBuildWonder"/>: is there a next stage, and can the player pay for it?</summary>
        public bool CheckCanBuildWonder()
        {
            CanBuildWonder = Board.NextStageCost is { } cost && PlanPayment(cost) != null;
            return CanBuildWonder;
        }

        /// <summary>
        /// Build the next wonder stage with a card from a provided cards list: pay for it, bury the card, apply the stage.
        /// </summary>
        /// <returns>true if the stage was built; otherwise false (nothing changes)</returns>
        public bool BuildWonder(Card card, List<Card> cardsList) => Resolve(PlanWonder(card, cardsList));

        // =========== Card related ===========
        /// <summary>
        /// Make a new list of playable cards from a given card list <br/>
        /// - playAllFree flag allows all cards in the list to be played for free
        /// </summary>
        /// <param name="cardList"></param>
        /// <param name="playAllFree">
        /// a flag that allows all cards in the list to be played for free (DiscardFree / FreeCardOnce effect)</param>
        /// <returns>
        /// a new List of playable cards
        /// </returns>
        public List<Card> GetPlayableCards(List<Card> cardList, bool playAllFree = false)
        {
            List<Card> playableCards = new List<Card>();
            foreach (Card card in cardList)
            {
                // the same card can't be built twice
                if (HasPlayedCard(card))
                    continue;

                if (playAllFree || CanChainBuildCard(card) || PlanPayment(card.Cost) != null)
                    playableCards.Add(card);
                else
                    Trace.Verbose("Player {PlayerId} -> CANT BUILD {cardName} -> can't pay for it.", Id, card.Name);
            }
            return playableCards;
        }

        /// <summary>
        /// Play a card from a provided cards list: pay for it (unless <paramref name="free_card"/> or chained), move it
        /// into the city and apply its effect.
        /// </summary>
        /// <returns>true if the card was built; otherwise false (nothing changes)</returns>
        public bool BuildStructure(Card card, List<Card> cards, bool free_card) =>
            Resolve(PlanBuild(ActionType.Build, card, cards, free_card));

        // =========== Moves: plan, place, settle (R5) ===========
        /// <summary>
        /// Step 1: checks a move against the current state and works out what it costs, changing nothing.
        /// Null if the rules don't allow it. <paramref name="source"/> is where the card comes from.
        /// </summary>
        public PlannedMove? PlanMove(ActionType action, Card card, List<Card> source) => action switch
        {
            ActionType.Build => PlanBuild(action, card, source, free: false),
            // Olympia A stage 2: one free card per era
            ActionType.BuildFreeOnce => Board.Id == WonderId.OlympiaA && Board.Stage >= 2 && FreeCardOnce
                ? PlanBuild(action, card, source, free: true) : null,
            ActionType.BuildWonder => PlanWonder(card, source),
            ActionType.Discard => source.Contains(card) ? new PlannedMove(this, action, card, source, PaymentPlan.Free, 0) : null,
            _ => null,
        };

        /// <summary>Step 1 for Halikarnassos: a free build from the discard pile while <see cref="DiscardFree"/> is on.</summary>
        public PlannedMove? PlanDiscardPileBuild(Card card, List<Card> discardPile)
        {
            bool halikarnassos = Board.Id == WonderId.HalikarnassosA && Board.Stage == 2 ||
                                 Board.Id == WonderId.HalikarnassosB && Board.Stage >= 1;
            return halikarnassos && DiscardFree && PlanBuild(ActionType.Build, card, discardPile, free: true) is { } move
                ? move with { FromDiscardPile = true }
                : null;
        }

        private PlannedMove? PlanBuild(ActionType action, Card card, List<Card> source, bool free)
        {
            if (HasPlayedCard(card) || !source.Contains(card))
            {
                Trace.Verbose("Player {PlayerId} -> FAILURE -> {CardName} is already built or not available.", Id, card.Name);
                return null;
            }
            if (free || CanChainBuildCard(card))
                return new PlannedMove(this, action, card, source, PaymentPlan.Free, 0);
            if (PlanPayment(card.Cost) is { } plan)
                return new PlannedMove(this, action, card, source, plan, card.Cost.TryGetValue(Resource.Coins, out int coins) ? coins : 0);

            Trace.Verbose("Player {PlayerId} -> FAILURE -> can't pay for {CardName}.", Id, card.Name);
            return null;
        }

        private PlannedMove? PlanWonder(Card card, List<Card> source)
        {
            if (CanBuildWonder && Board.NextStageCost is { } cost && PlanPayment(cost) is { } plan && source.Contains(card))
                return new PlannedMove(this, ActionType.BuildWonder, card, source, plan, 0);

            Trace.Verbose("FAILED to build {WonderName} stage {stage} by Player {PlayerId} (CanBuildWonder: {CanBuildWonder})",
                Board.Name, Board.Stage + 1, Id, CanBuildWonder);
            return null;
        }

        /// <summary>
        /// Step 2: the player pays their own coins (to the neighbours and the bank) and the card leaves where it came
        /// from. A built card goes into the city. Nobody receives anything yet.
        /// </summary>
        public void Place(PlannedMove move)
        {
            Resources[Resource.Coins] -= move.Payment.Total + move.CoinCost;
            move.Source.Remove(move.Card);
            // The card is in the city before its effect applies, so the Lighthouse counts itself.
            // Sources: https://rules.dized.com/7-wonders/rule/question/19075b63-591e-4e99-8dfc-5fa34b726973,
            //          https://rules.dized.com/7-wonders/rule/symbols/third-age-cards
            if (move.Action is ActionType.Build or ActionType.BuildFreeOnce)
                PlayedCards.Add(move.Card);
        }

        /// <summary>
        /// Step 3: the neighbours receive their trade coins and the move takes effect: the card's effect, the wonder
        /// stage, or 3 coins for a discard. A discarded card goes to the discard pile in <see cref="Game.ResolveMoves"/>.
        /// </summary>
        public void Settle(PlannedMove move)
        {
            PlayerWest.Resources[Resource.Coins] += move.Payment.ToWest;
            PlayerEast.Resources[Resource.Coins] += move.Payment.ToEast;
            switch (move.Action)
            {
                case ActionType.Build:
                case ActionType.BuildFreeOnce:
                    ApplyCardEffect(move.Card);
                    // used up: Olympia A's is re-armed at the start of the next era
                    if (move.Action == ActionType.BuildFreeOnce)
                        FreeCardOnce = false;
                    if (move.FromDiscardPile)
                        DiscardFree = false;
                    break;
                case ActionType.BuildWonder:
                    Board.AddStage(this);
                    CanBuildWonder = false;
                    break;
                case ActionType.Discard:
                    Resources[Resource.Coins] += 3;
                    break;
            }
        }

        /// <summary>One player's move on its own (outside a normal turn, and in tests): plan, place, settle.</summary>
        private bool Resolve(PlannedMove? move)
        {
            if (move == null)
                return false;
            Place(move);
            Settle(move);
            return true;
        }

        /// <summary>A card's effect when it is built: production, coins, shields, science or a trading flag.</summary>
        protected void ApplyCardEffect(Card card)
        {
            CardId cardId = card.Id;
            switch (card.Type)
            {
                case CardType.Materials:
                    if (cardId == CardId.LumberYard)
                    {
                        Resources[Resource.Wood] += 1;
                    }
                    else if (cardId == CardId.StonePit)
                    {
                        Resources[Resource.Stone] += 1;
                    }
                    else if (cardId == CardId.ClayPool)
                    {
                        Resources[Resource.Clay] += 1;
                    }
                    else if (cardId == CardId.OreVein)
                    {
                        Resources[Resource.Ore] += 1;
                    }
                    else if (cardId == CardId.TreeFarm)
                    {
                        // wood or clay (used when paying, see PaymentPlanner)
                    }
                    else if (cardId == CardId.Excavation)
                    {
                        // stone or clay (used when paying, see PaymentPlanner)
                    }
                    else if (cardId == CardId.ClayPit)
                    {
                        // clay or ore (used when paying, see PaymentPlanner)
                    }
                    else if (cardId == CardId.TimberYard)
                    {
                        // stone or wood (used when paying, see PaymentPlanner)
                    }
                    else if (cardId == CardId.ForestCave)
                    {
                        // wood or ore (used when paying, see PaymentPlanner)
                    }
                    else if (cardId == CardId.Mine)
                    {
                        // ore or stone (used when paying, see PaymentPlanner)
                    }
                    else if (cardId == CardId.Sawmill)
                    {
                        Resources[Resource.Wood] += 2;
                    }
                    else if (cardId == CardId.Quarry)
                    {
                        Resources[Resource.Stone] += 2;
                    }
                    else if (cardId == CardId.Brickyard)
                    {
                        Resources[Resource.Clay] += 2;
                    }
                    else if (cardId == CardId.Foundry)
                    {
                        Resources[Resource.Ore] += 2;
                    }
                    break;

                case CardType.Manufactured:
                    if (cardId == CardId.Loom || cardId == CardId.Loom2)
                    {
                        Resources[Resource.Loom]++;
                    }
                    else if (cardId == CardId.Glassworks || cardId == CardId.Glassworks2)
                    {
                        Resources[Resource.Glass]++;
                    }
                    else if (cardId == CardId.Press || cardId == CardId.Press2)
                    {
                        Resources[Resource.Papyrus]++;
                    }
                    break;

                case CardType.Commercial:
                    if (cardId == CardId.Tavern)
                    {
                        Resources[Resource.Coins] += 5;
                    }
                    else if (cardId == CardId.EastTradingPost)
                    {
                        RawCheapEast = true;
                    }
                    else if (cardId == CardId.WestTradingPost)
                    {
                        RawCheapWest = true;
                    }
                    else if (cardId == CardId.Marketplace)
                    {
                        ManufCheap = true;
                    }
                    else if (cardId == CardId.Forum)
                    {
                        // loom or glass or papyrus (used when paying, see PaymentPlanner)
                    }
                    else if (cardId == CardId.Caravansery)
                    {
                        // clay or stone or ore or wood (used when paying, see PaymentPlanner)
                    }
                    else if (cardId == CardId.Vineyard)
                    {
                        int brownCards = PlayerEast.AmountOfType(CardType.Materials)
                                        + PlayerWest.AmountOfType(CardType.Materials)
                                        + AmountOfType(CardType.Materials);
                        Resources[Resource.Coins] += 1 * brownCards;
                    }
                    else if (cardId == CardId.Bazar)
                    {
                        int greyCards = PlayerEast.AmountOfType(CardType.Manufactured)
                                        + PlayerWest.AmountOfType(CardType.Manufactured)
                                        + AmountOfType(CardType.Manufactured);
                        Resources[Resource.Coins] += 2 * greyCards;
                    }
                    else if (cardId == CardId.Haven)
                    {
                        int brownCards = AmountOfType(CardType.Materials);
                        Resources[Resource.Coins] += 1 * brownCards;
                    }
                    else if (cardId == CardId.Lighthouse)
                    {
                        int yellowCards = AmountOfType(CardType.Commercial);
                        Resources[Resource.Coins] += 1 * yellowCards;
                    }
                    else if (cardId == CardId.ChamberOfCommerce)
                    {
                        int greyCards = AmountOfType(CardType.Manufactured);
                        Resources[Resource.Coins] += 2 * greyCards;
                    }
                    else if (cardId == CardId.Arena)
                    {
                        int stages = Board.Stage;
                        Resources[Resource.Coins] += 3 * stages;
                    }
                    break;

                case CardType.Military:
                    if (cardId == CardId.Stockade || cardId == CardId.Barracks || cardId == CardId.GuardTower)
                    {
                        Resources[Resource.Shields] += 1;
                    }
                    else if (cardId == CardId.Walls || cardId == CardId.TrainingGround ||
                               cardId == CardId.Stables || cardId == CardId.ArcheryRange)
                    {
                        Resources[Resource.Shields] += 2;
                    }
                    else if (cardId == CardId.Fortifications || cardId == CardId.Circus ||
                               cardId == CardId.Arsenal || cardId == CardId.SiegeWorkshop)
                    {
                        Resources[Resource.Shields] += 3;
                    }
                    break;

                case CardType.Scientific:
                    if (cardId == CardId.Workshop || cardId == CardId.Laboratory ||
                        cardId == CardId.Observatory || cardId == CardId.Study)
                    {
                        Resources[Resource.Gear]++;
                    }
                    else if (cardId == CardId.Apothecary || cardId == CardId.Dispensary ||
                        cardId == CardId.Lodge || cardId == CardId.Academy)
                    {
                        Resources[Resource.Compass]++;
                    }
                    else if (cardId == CardId.Scriptorium || cardId == CardId.Library ||
                        cardId == CardId.School || cardId == CardId.University)
                    {
                        Resources[Resource.Tablet]++;
                    }
                    break;

                case CardType.Guild:
                    if (cardId == CardId.Scientists)
                    {
                        ExtraScience = true;
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>The no-duplicates rule: true if the city already has a card with this name.</summary>
        private bool HasPlayedCard(Card card)
        {
            foreach (Card playedCard in PlayedCards)
                if (playedCard.SameNameAs(card))
                    return true;

            return false;
        }

        private bool CanChainBuildCard(Card card)
        {
            CardId freeWithId = card.FreeWithId;

            if (freeWithId != CardId.None)
            {
                // Treating exception:
                // Forum can be constructed for free if the player has either the East Trading Post
                // or the West Trading Post (raw_cheap_east or raw_cheap_west).
                if (freeWithId == CardId.AnyTradingPost && (RawCheapEast || RawCheapWest))
                    return true;

                // General case:
                foreach (Card playedCard in PlayedCards)
                {
                    if (playedCard.Id == freeWithId)
                        return true;
                }
            }

            return false;
        }

        public int AmountOfType(CardType cardType)
        {
            int amount = 0;
            foreach (Card card in PlayedCards)
                if (card.Type == cardType)
                    amount++;

            return amount;
        }

        /// <summary>
        /// Discard a card from HandCards and gain 3 coins
        /// </summary>
        public void Discard(Card card)
        {
            if (!Resolve(PlanMove(ActionType.Discard, card, HandCards)))
                throw new InvalidOperationException($"Seat {Id} can't discard {card.Name}: it isn't in their hand.");
        }

        // =========== Wonder effect related ===========
        /// <summary>
        /// If DiscardFree effect is active, builds a card from the discarded cards list passed into the function for free.
        /// </summary>
        /// <returns>true if the card was built; otherwise false (nothing changes)</returns>
        public bool BuildDiscardFree(Card card, List<Card> discardedCards) => Resolve(PlanDiscardPileBuild(card, discardedCards));

        /// <summary>
        /// If FreeCardOnce Olympia A stage 2 effect is active, builds a card from the cards list passed into the function
        /// for free. Works once per era.
        /// </summary>
        public bool BuildHandFree(Card card, List<Card> cardList) => Resolve(PlanMove(ActionType.BuildFreeOnce, card, cardList));

        // =========== Resource related ===========
        // =========== Paying ===========
        /// <summary>The cheapest way to pay <paramref name="cost"/>, or null if the player can't (see <see cref="PaymentPlanner"/>).</summary>
        public PaymentPlan? PlanPayment(IReadOnlyDictionary<Resource, int> cost) => PaymentPlanner.Plan(this, cost);

        /// <summary>Pays the neighbours what the plan says, all at once (a turn pays in <see cref="Place"/> and <see cref="Settle"/>).</summary>
        public void Pay(PaymentPlan plan)
        {
            Resources[Resource.Coins] -= plan.Total;
            PlayerWest.Resources[Resource.Coins] += plan.ToWest;
            PlayerEast.Resources[Resource.Coins] += plan.ToEast;
        }

        /// <summary>
        /// How much of <paramref name="needed"/> this player could produce of a resource: its fixed production plus
        /// every either/or source that can give it (for a neighbour, only the brown cards they could sell).
        /// Read-only; the heuristics use it to judge what is missing.
        /// </summary>
        int CountProducible(Resource resource, int needed, bool isNeighbor)
        {
            int count = Resources[resource];
            if (count >= needed) return needed;

            foreach (Card card in PlayedCards)
                if ((PaymentPlanner.EitherOrMask(card.Id, isNeighbor) & (1 << (int)resource)) != 0)
                    count++;
            if (!isNeighbor && (int)resource <= (int)Resource.Stone && HasExtraRaw()) count++;
            if (!isNeighbor && resource >= Resource.Loom && resource <= Resource.Papyrus && HasExtraManuf()) count++;

            return Math.Min(needed, count);
        }

        /// <summary>Alexandria A stage 2 / B stage 1: one raw resource of the player's choice each turn.</summary>
        internal bool HasExtraRaw() =>
            Board.Id == WonderId.AlexandriaA && Board.Stage >= 2 || Board.Id == WonderId.AlexandriaB && Board.Stage >= 1;

        /// <summary>Alexandria B stage 2: one manufactured good of the player's choice each turn.</summary>
        internal bool HasExtraManuf() => Board.Id == WonderId.AlexandriaB && Board.Stage >= 2;

        // =========== Scoring related ===========
        // =========== Pre scoring related ===========
        /// <summary>
        /// Give Victory and Defeat tokens to Player based on era by battling each neighbor
        /// </summary>
        /// <param name="era"></param>
        public void Battle(int era)
        {
            int current_era_value = 1;
            // Age I   ->  +1 victory token
            // Age II  ->  +3 victory tokens
            // Age III ->  +5 victory tokens
            switch (era)
            {
                case 2:
                    current_era_value = 3;
                    break;
                case 3:
                    current_era_value = 5;
                    break;
                default:
                    break;
            }

            int this_shields = Resources[Resource.Shields];

            // Battle with east neighbor
            if (this_shields > PlayerEast.Resources[Resource.Shields])
            {
                VictoryTokens += current_era_value;
            }
            else if (this_shields < PlayerEast.Resources[Resource.Shields])
            {
                DefeatTokens += 1;
            }

            // Battle with west neighbor
            if (this_shields > PlayerWest.Resources[Resource.Shields])
            {
                VictoryTokens += current_era_value;
            }
            else if (this_shields < PlayerWest.Resources[Resource.Shields])
            {
                DefeatTokens += 1;
            }
        }

        /// <summary>
        /// Olympia B stage 3: at the end of the game, copy one guild built by a neighbour, as if built. <br/>
        /// - each candidate is scored with the real CalculateScore(); the best one is kept (ties go to the first candidate, west then east) <br/>
        /// - the copy stays in PlayedCards, so Shipowners counts it; copying Scientists sets ExtraScience <br/>
        /// - a guild the player already has is skipped (no two identical structures) <br/>
        /// - should be called before calculating scores <br/>
        /// Sources: https://rules.dized.com/7-wonders/rule/wonder-boards/the-statue-of-zeus-in-olympia,
        ///          https://rules.dized.com/7-wonders/rule/symbols/guilds
        /// </summary>
        /// <returns>
        /// true if a guild was copied; otherwise false
        /// </returns>
        public bool CopyGuild()
        {
            if (Board.Id != WonderId.OlympiaB || Board.Stage < 3)
                return false;

            List<Card> candidates = PlayerWest.PlayedCards.Concat(PlayerEast.PlayedCards)
                .Where(card => card.Type == CardType.Guild && !HasPlayedCard(card))
                .Distinct()
                .ToList();

            if (candidates.Count == 0)
            {
                Trace.Verbose("Seat {Seat} -> no guild to copy", Id);
                return false;
            }

            int victoryPointsBckp = VictoryPoints;
            bool extraScienceBckp = ExtraScience;

            Card? bestGuild = null;
            int bestScore = int.MinValue;
            foreach (Card guild in candidates)
            {
                PlayedCards.Add(guild);
                if (guild.Id == CardId.Scientists)
                    ExtraScience = true;

                int score = CalculateScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    bestGuild = guild;
                }

                PlayedCards.RemoveAt(PlayedCards.Count - 1);
                ExtraScience = extraScienceBckp;
            }
            VictoryPoints = victoryPointsBckp;

            PlayedCards.Add(bestGuild!);
            if (bestGuild!.Id == CardId.Scientists)
                ExtraScience = true;

            Trace.Verbose("Seat {Seat} -> copies guild {Guild}", Id, bestGuild.Name);
            return true;
        }

        // =========== End of game scoring related ===========
        /// <summary>
        /// Calculate Civilian (Blue) card VP score for Player
        /// </summary>
        /// <returns></returns>
        public int CalculateCivilianScore()
        {
            // Each Civilian (blue) structure gives a fixed number of VPs.
            int civil_score = 0;

            foreach (Card card in PlayedCards)
            {
                if (card.Type != CardType.Civilian)
                    continue;

                switch (card.Id)
                {
                    case CardId.Altar:
                    case CardId.Theater:
                        civil_score += 2;
                        break;

                    case CardId.Pawnshop:
                    case CardId.Baths:
                    case CardId.Temple:
                        civil_score += 3;
                        break;

                    case CardId.Courthouse:
                    case CardId.Statue:
                        civil_score += 4;
                        break;

                    case CardId.Aqueduct:
                    case CardId.Gardens:
                        civil_score += 5;
                        break;

                    case CardId.TownHall:
                    case CardId.Senate:
                        civil_score += 6;
                        break;

                    case CardId.Pantheon:
                        civil_score += 7;
                        break;

                    case CardId.Palace:
                        civil_score += 8;
                        break;
                }
            }

            return civil_score;
        }

        /// <summary>
        /// Calculate Commercial (Yellow) card VP score for Player
        /// </summary>
        /// <returns></returns>
        public int CalculateCommercialScore()
        {
            int comm_score = 0;

            foreach (Card card in PlayedCards)
            {
                if (card.Type != CardType.Commercial)
                    continue;

                switch (card.Id)
                {
                    case CardId.Haven:
                        // +1 VP per Raw Material CARD in your own city.
                        comm_score += AmountOfType(CardType.Materials);
                        break;

                    case CardId.Lighthouse:
                        // +1 VP per Commercial Structure CARD in your own city.
                        comm_score += AmountOfType(CardType.Commercial);
                        break;

                    case CardId.ChamberOfCommerce:
                        // +2 VP per Manufactured Good CARD in your own city.
                        comm_score += AmountOfType(CardType.Manufactured) * 2;
                        break;

                    case CardId.Arena:
                        // +1 VP Coin per Wonder stage constructed in your own city.
                        comm_score += Board.Stage;
                        break;
                }
            }

            return comm_score;
        }

        /// <summary>
        /// Calculate Money VP score for Player (3 coins = 1 VP)
        /// </summary>
        /// <returns></returns>
        public int CalculateMoneyScore()
        {
            return (int)decimal.Floor(Resources[Resource.Coins] / 3);
        }

        /// <summary>
        /// Calculate Guild (Purple) card VP score for Player
        /// </summary>
        /// <returns></returns>
        public int CalculateGuildScore()
        {
            // The guilds (purple) provide several means of gaining victory points, most of
            // which are based on the types of structure a player and/or his neighbors have built.
            int guild_score = 0;

            foreach (Card card in PlayedCards)
            {
                if (card.Type != CardType.Guild)
                    continue;

                switch (card.Id)
                {
                    case CardId.Workers:
                        guild_score += PlayerEast.AmountOfType(CardType.Materials)
                                     + PlayerWest.AmountOfType(CardType.Materials);
                        break;

                    case CardId.Craftsmens:
                        guild_score += 2 * (PlayerEast.AmountOfType(CardType.Manufactured)
                                     + PlayerWest.AmountOfType(CardType.Manufactured));
                        break;

                    case CardId.Traders:
                        guild_score += PlayerEast.AmountOfType(CardType.Commercial)
                                     + PlayerWest.AmountOfType(CardType.Commercial);
                        break;

                    case CardId.Philosophers:
                        guild_score += PlayerEast.AmountOfType(CardType.Scientific)
                                     + PlayerWest.AmountOfType(CardType.Scientific);
                        break;

                    case CardId.Spies:
                        guild_score += PlayerEast.AmountOfType(CardType.Military)
                                     + PlayerWest.AmountOfType(CardType.Military);
                        break;

                    case CardId.Magistrates:
                        guild_score += PlayerEast.AmountOfType(CardType.Civilian)
                                     + PlayerWest.AmountOfType(CardType.Civilian);
                        break;

                    case CardId.Shipowners:
                        guild_score += AmountOfType(CardType.Materials)
                                     + AmountOfType(CardType.Manufactured)
                                     + AmountOfType(CardType.Guild);
                        break;

                    case CardId.Strategists:
                        guild_score += PlayerEast.DefeatTokens
                                     + PlayerWest.DefeatTokens;
                        break;

                    case CardId.Scientists:
                        // computed in CalculateScientificScore()
                        break;

                    case CardId.Builders:
                        guild_score += PlayerEast.Board.Stage
                                     + PlayerWest.Board.Stage
                                     + Board.Stage;
                        break;
                }
            }

            return guild_score;
        }

        /// <summary>
        /// Calculate Military VP score for Player
        /// </summary>
        /// <returns></returns>
        public int CalculateMilitaryScore()
        {
            return VictoryTokens - DefeatTokens;
        }

        /// <summary>
        /// Calculate Scientific VP score for Player including extra science structures
        /// </summary>
        /// <returns></returns>
        public int CalculateScientificScore()
        {
            int gear = Resources[Resource.Gear],
                tablet = Resources[Resource.Tablet],
                compass = Resources[Resource.Compass];

            // calculate extra science
            int sci_extra = 0;
            if (ExtraScience)
                sci_extra++;

            WonderId id = Board.Id;
            int stage = Board.Stage;
            if (id == WonderId.BabylonA && stage >= 2 ||
                id == WonderId.BabylonB && stage >= 3)
            {
                sci_extra++;
            }

            return BestScienceScore(gear, compass, tablet, sci_extra);
        }

        /// <summary>
        /// Science VP with each wildcard placed on the best symbol, found by trying every placement (3^k, k &lt;= 2).
        /// </summary>
        public static int BestScienceScore(int gear, int compass, int tablet, int wildcards)
        {
            if (wildcards == 0)
                return ScienceScore(gear, compass, tablet);

            return Math.Max(BestScienceScore(gear + 1, compass, tablet, wildcards - 1),
                   Math.Max(BestScienceScore(gear, compass + 1, tablet, wildcards - 1),
                            BestScienceScore(gear, compass, tablet + 1, wildcards - 1)));
        }

        /// <summary>
        /// Rulebook science VP: each symbol type scores its count squared, and each complete set of three different symbols scores 7.
        /// </summary>
        public static int ScienceScore(int gear, int compass, int tablet)
        {
            int completedSets = Math.Min(gear, Math.Min(compass, tablet));
            return gear * gear + compass * compass + tablet * tablet + 7 * completedSets;
        }

        /// <summary>
        /// Calculate Wonder VP score for Player
        /// </summary>
        /// <returns></returns>
        public int CalculateWonderScore()
        {
            if (Board.Stage > 0)
                return Board.WonderPoints;
            return 0;
        }

        /// <summary>
        /// Update total Victory Points for Player
        /// </summary>
        /// <returns>
        /// player Victory Points
        /// </returns>
        public int CalculateScore()
        {
            int civil_score = CalculateCivilianScore();
            int commercial_score = CalculateCommercialScore();
            int money_score = CalculateMoneyScore();
            int guild_score = CalculateGuildScore();
            int science_score = CalculateScientificScore();
            int military_score = CalculateMilitaryScore();
            int wonder_score = CalculateWonderScore();

            int total_score = civil_score + commercial_score + money_score + 
                guild_score + science_score + military_score + wonder_score;

            VictoryPoints = total_score;

            return VictoryPoints;
        }



        // =========== Gameplay related functions ===========
        /// <summary>
        /// Choose a card for the turn from the list <br/>
        /// * caller is responsible that all cards in the list are playable <br/>
        /// - game and action are used to evaluate card choice <br/>
        /// - if return is null the move should be skipped!
        /// </summary>
        /// <param name="game"></param>
        /// <param name="action"></param>
        /// <param name="cardsList"></param>
        /// <returns>
        /// chosen Card for the turn or null if cardList was empty
        /// </returns>
        public abstract Card? ChooseMoveCard(Game game, ActionType action, List<Card> cardsList);

        /// <summary>
        /// Choose a move for the turn <br/>
        /// - game is used to evaluate move and card choice
        /// </summary>
        /// <param name="game"></param>
        public abstract void ChooseMoveCommand(Game game);

        // =========== Deterministic heuristics ===========
        public Card? Age3BuildHeuristic(List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;

            foreach (Card card in cardsList)
            {
                score = ScoreAfterPlayingCard(3, card);

                if (score > maxScore)
                {
                    bestCard = card;
                    maxScore = score;
                }
                else if (!isNeighbor && score == maxScore)
                {
                    // if cards score is equal to max score then choose a card that would be most usefull for neighbor in next turn
                    Player nextNeighbor = this.PlayerWest;
                    List<Card> nextNeighborPlayableCards = nextNeighbor.GetPlayableCards(cardsList);
                    Card? betterBestCard = nextNeighbor.Age3BuildHeuristic(nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            return bestCard;
        }

        public Card? Age2BuildHeuristic(List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;

            foreach (Card card in cardsList)
            {
                if (card.Id == CardId.Vineyard &&
                    this.Resources[Resource.Coins] <= 3)
                {
                    score = ScoreAfterPlayingCard(2, null);
                    // add score for gaining money with extra weight 
                    int cardAmount = this.AmountOfType(CardType.Materials) + 
                        this.PlayerEast.AmountOfType(CardType.Materials) + 
                        this.PlayerWest.AmountOfType(CardType.Materials);
                    score += 1 * cardAmount * 2;
                }
                else if (card.Id == CardId.Bazar &&
                    this.Resources[Resource.Coins] <= 3)
                {
                    score = ScoreAfterPlayingCard(2, null);
                    // add score for gaining money with extra weight 
                    int cardAmount = this.AmountOfType(CardType.Manufactured) +
                        this.PlayerEast.AmountOfType(CardType.Manufactured) +
                        this.PlayerWest.AmountOfType(CardType.Manufactured);
                    score += 2 * cardAmount * 2;
                }
                else if (card.Id == CardId.Forum)
                {
                    score = ScoreAfterPlayingCard(2, null);

                    // get amounts of potencially producable manufectured goods resources
                    int loomAmount = this.Resources[Resource.Loom] +
                        this.PlayerEast.Resources[Resource.Loom] +
                        this.PlayerWest.Resources[Resource.Loom];
                    bool extraManuf = this.HasExtraManuf();
                    if (loomAmount == 0 && extraManuf)
                    {
                        loomAmount++;
                        extraManuf = false;
                    }

                    int glassAmount = this.Resources[Resource.Glass] +
                        this.PlayerEast.Resources[Resource.Glass] +
                        this.PlayerWest.Resources[Resource.Glass];
                    if (glassAmount == 0 && extraManuf)
                    {
                        glassAmount++;
                        extraManuf = false;
                    }

                    int papyrusAmount = this.Resources[Resource.Papyrus] +
                        this.PlayerEast.Resources[Resource.Papyrus] +
                        this.PlayerWest.Resources[Resource.Papyrus];
                    if (papyrusAmount == 0 && extraManuf)
                        papyrusAmount++;

                    // check if enough manufactured goods for wonder building
                    if (this.Board.HighestCosts[Resource.Loom] > loomAmount || 
                        this.Board.HighestCosts[Resource.Glass] > glassAmount ||
                        this.Board.HighestCosts[Resource.Papyrus] > papyrusAmount)
                    {
                        score += 10;     // slightly better than double raw resource when its needed for wonder building
                    }
                    // check if any maufactured goods 
                    else if (loomAmount < 1 &&
                            glassAmount < 1 &&
                            papyrusAmount < 1)
                    {
                        score += 9;     // same as double resource when not needed for wonder building, but missing any or all of manufactured goods 
                    }
                }
                else if (card.Id == CardId.Caravansery)
                {
                    score = ScoreAfterPlayingCard(2, null);

                    // check if enough resources to build wonder before resource card
                    int neededWood = this.Board.HighestCosts[Resource.Wood],
                        neededStone = this.Board.HighestCosts[Resource.Stone],
                        neededClay = this.Board.HighestCosts[Resource.Clay],
                        neededOre = this.Board.HighestCosts[Resource.Ore];

                    int producedWoodThis = this.CountProducible(Resource.Wood, neededWood, false);
                    int producedWoodWest = this.PlayerWest.CountProducible(Resource.Wood, neededWood, true);
                    int producedWoodEast = this.PlayerEast.CountProducible(Resource.Wood, neededWood, true);
                    neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                    int producedStoneThis = this.CountProducible(Resource.Stone, neededStone, false);
                    int producedStoneWest = this.PlayerWest.CountProducible(Resource.Stone, neededStone, true);
                    int producedStoneEast =  this.PlayerEast.CountProducible(Resource.Stone, neededStone, true);
                    neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                    int producedClayThis = this.CountProducible(Resource.Clay, neededClay, false);
                    int producedClayWest = this.PlayerWest.CountProducible(Resource.Clay, neededClay, true);
                    int producedClayEast =  this.PlayerEast.CountProducible(Resource.Clay, neededClay, true);
                    neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                    int producedOreThis = this.CountProducible(Resource.Ore, neededOre, false);
                    int producedOreWest = this.PlayerWest.CountProducible(Resource.Ore, neededOre, true);
                    int producedOreEast =  this.PlayerEast.CountProducible(Resource.Ore, neededOre, true);
                    neededOre = neededOre - producedOreThis - producedOreWest - producedOreEast;

                    if (neededWood > 0 || (producedWoodThis + producedWoodWest + producedWoodEast) < 2 ||
                        neededStone > 0 || (producedStoneThis + producedStoneWest + producedStoneEast) < 2 ||
                        neededClay > 0 || (producedClayThis + producedClayWest + producedClayEast) < 2 ||
                        neededOre > 0 || (producedOreThis + producedOreWest + producedOreEast) < 2)
                    {
                        score += 11;        // better than multichoice manufactured goods resources and better than double raw resources
                    }
                }
                else if (card.Type == CardType.Materials)
                {
                    score = ScoreAfterPlayingCard(2, null);

                    // check if enough resources to build wonder before resource card
                    int neededWood = this.Board.HighestCosts[Resource.Wood],
                        neededStone = this.Board.HighestCosts[Resource.Stone],
                        neededClay = this.Board.HighestCosts[Resource.Clay],
                        neededOre = this.Board.HighestCosts[Resource.Ore];

                    int producedWoodThis = this.CountProducible(Resource.Wood, neededWood, false);
                    int producedWoodWest = this.PlayerWest.CountProducible(Resource.Wood, neededWood, true);
                    int producedWoodEast = this.PlayerEast.CountProducible(Resource.Wood, neededWood, true);
                    neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                    int producedStoneThis = this.CountProducible(Resource.Stone, neededStone, false);
                    int producedStoneWest = this.PlayerWest.CountProducible(Resource.Stone, neededStone, true);
                    int producedStoneEast = this.PlayerEast.CountProducible(Resource.Stone, neededStone, true);
                    neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                    int producedClayThis = this.CountProducible(Resource.Clay, neededClay, false);
                    int producedClayWest = this.PlayerWest.CountProducible(Resource.Clay, neededClay, true);
                    int producedClayEast = this.PlayerEast.CountProducible(Resource.Clay, neededClay, true);
                    neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                    int producedOreThis = this.CountProducible(Resource.Ore, neededOre, false);
                    int producedOreWest = this.PlayerWest.CountProducible(Resource.Ore, neededOre, true);
                    int producedOreEast = this.PlayerEast.CountProducible(Resource.Ore, neededOre, true);
                    neededOre = neededOre - producedOreThis - producedOreWest - producedOreEast;

                    if (neededWood > 0 ||
                        neededStone > 0 ||
                        neededClay > 0 ||
                        neededOre > 0)
                    {
                        if (card.Id == CardId.Sawmill)
                        {
                            neededWood -= 2;
                            if (this.Board.HighestCosts[Resource.Wood] != 0 && neededWood > 0)
                            {
                                score += 9;     // slightly worse than multichoice manufactured goods resources when they are needed for wonder building
                            }
                            else if ((producedWoodThis + producedWoodWest + producedWoodEast + 2) < 2)
                            {
                                score += 10;    // same as multichoice manufactured goods resource when its needed for wonder building
                            }
                        }
                        else if (card.Id == CardId.Quarry)
                        {
                            neededStone -= 2;
                            if (this.Board.HighestCosts[Resource.Stone] != 0 && neededStone > 0)
                            {
                                score += 9;     // slightly worse than multichoice manufactured goods resources when they are needed for wonder building
                            }
                            else if ((producedStoneThis + producedStoneWest + producedStoneEast + 2) < 2)
                            {
                                score += 10;    // same as multichoice manufactured goods resource when its needed for wonder building
                            }
                        }
                        else if (card.Id == CardId.Brickyard)
                        {
                            neededClay -= 2;
                            if (this.Board.HighestCosts[Resource.Clay] != 0 && neededClay > 0)
                            {
                                score += 9;     // slightly worse than multichoice manufactured goods resources when they are needed for wonder building
                            }
                            else if ((producedClayThis + producedClayWest + producedClayEast + 2) < 2)
                            {
                                score += 10;    // same as multichoice manufactured goods resource when its needed for wonder building
                            }
                        }
                        else if (card.Id == CardId.Foundry)
                        {
                            neededOre -= 2;
                            if (this.Board.HighestCosts[Resource.Ore] != 0 && neededOre > 0)
                            {
                                score += 9;     // slightly worse than multichoice manufactured goods resources when they are needed for wonder building
                            }
                            else if ((producedOreThis + producedOreWest + producedOreEast + 2) < 2)
                            {
                                score += 10;    // same as multichoice manufactured goods resource when its needed for wonder building
                            }
                        }
                    }
                }
                else if (card.Type == CardType.Manufactured)
                {
                    score = ScoreAfterPlayingCard(2, null);

                    // get amounts of potencially producable manufectured goods resources
                    int loomAmount = this.Resources[Resource.Loom] +
                        this.PlayerEast.Resources[Resource.Loom] +
                        this.PlayerWest.Resources[Resource.Loom];

                    int glassAmount = this.Resources[Resource.Glass] +
                        this.PlayerEast.Resources[Resource.Glass] +
                        this.PlayerWest.Resources[Resource.Glass];

                    int papyrusAmount = this.Resources[Resource.Papyrus] +
                        this.PlayerEast.Resources[Resource.Papyrus] +
                        this.PlayerWest.Resources[Resource.Papyrus];

                    // choose extra manuf for first non 0 wonder cost and try then
                    if (this.Board.HighestCosts[Resource.Loom] != 0 && this.Board.HighestCosts[Resource.Loom] > loomAmount && this.HasExtraManuf())
                        loomAmount++;
                    else if (this.Board.HighestCosts[Resource.Glass] != 0 && this.Board.HighestCosts[Resource.Glass] > glassAmount && this.HasExtraManuf())
                        glassAmount++;
                    else if (this.Board.HighestCosts[Resource.Papyrus] != 0 && this.Board.HighestCosts[Resource.Papyrus] > papyrusAmount && this.HasExtraManuf())
                        papyrusAmount++;

                    if (card.Name.Equals("Loom"))
                    {
                        // check if enough loom for wonder building
                        if (this.Board.HighestCosts[Resource.Loom] > loomAmount)
                        {
                            score += 8;     // slightly worse than double raw resource when its needed for wonder building
                        }
                        // check if any manufactured good 
                        else if (loomAmount < 1 && glassAmount < 1 && papyrusAmount < 1)
                        {
                            score += 8;     // slightly worse than double raw resource when its needed for wonder building 
                        }
                    }
                    else if (card.Name.Equals("Glassworks"))
                    {
                        // check if enough glass for wonder building
                        if (this.Board.HighestCosts[Resource.Glass] > glassAmount)
                        {
                            score += 8;     // slightly worse than double raw resource when its needed for wonder building
                        }
                        // check if any manufactured good 
                        else if (loomAmount < 1 && glassAmount < 1 && papyrusAmount < 1)
                        {
                            score += 8;     // slightly worse than double raw resource when its needed for wonder building 
                        }
                    }
                    else if (card.Name.Equals("Press"))
                    {
                        // check if enough papyrus for wonder building
                        if (this.Board.HighestCosts[Resource.Papyrus] > papyrusAmount)
                        {
                            score += 8;     // slightly worse than double raw resource when its needed for wonder building
                        }
                        // check if any manufactured good 
                        else if (loomAmount < 1 && glassAmount < 1 && papyrusAmount < 1)
                        {
                            score += 8;     // slightly worse than double raw resource when its needed for wonder building 
                        }
                    }
                }
                else if (card.Type == CardType.Scientific)
                {
                    score = ScoreAfterPlayingCard(2, card);
                    if (this.AmountOfType(CardType.Scientific) < 1 &&
                        this.AmountOfType(CardType.Materials) > 1)
                        score += 10;
                    else
                        score += 2.3;
                }
                else
                    score = ScoreAfterPlayingCard(2, card);

                if (score > maxScore)
                {
                    bestCard = card;
                    maxScore = score;
                }
                else if (!isNeighbor && score == maxScore)
                {
                    // if cards score is equal to max score then choose a card that would be most usefull for neighbor in next turn
                    Player nextNeighbor = this.PlayerEast;
                    List<Card> nextNeighborPlayableCards = nextNeighbor.GetPlayableCards(cardsList);
                    Card? betterBestCard = nextNeighbor.Age2BuildHeuristic(nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            return bestCard;
        }

        public Card? Age1BuildHeuristic(List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;  // must be less than possible negative score from military loss

            foreach (Card card in cardsList)
            {
                if (card.Id == CardId.EastTradingPost)
                {
                    score = ScoreAfterPlayingCard(1, null);
                    int eastRawAmount = this.PlayerEast.AmountOfType(CardType.Materials);
                    int thisRawAmount = this.AmountOfType(CardType.Materials);
                    if (this.Board.Production <= Resource.Stone)
                        thisRawAmount++;
                    if (this.PlayerEast.Board.Production <= Resource.Stone)
                        eastRawAmount++;

                    if (thisRawAmount <= 2 &&
                        eastRawAmount >= 1)
                        score += 2 + eastRawAmount;     // slightly better than any war or science and better than resources 
                }
                else if (card.Id == CardId.WestTradingPost)
                {
                    score = ScoreAfterPlayingCard(1, null);
                    int westRawAmount = this.PlayerWest.AmountOfType(CardType.Materials);
                    int thisRawAmount = this.AmountOfType(CardType.Materials);
                    if (this.Board.Production <= Resource.Stone)
                        thisRawAmount++;
                    if (this.PlayerWest.Board.Production <= Resource.Stone)
                        westRawAmount++;

                    if (thisRawAmount <= 2 &&
                        westRawAmount >= 1)
                        score += 2.3 + westRawAmount;     // slightly better than any war or science and better than resources 
                }
                else if (card.Id == CardId.Marketplace)
                {
                    score = ScoreAfterPlayingCard(1, null);
                    int eastManufAmount = this.PlayerEast.AmountOfType(CardType.Manufactured);
                    int westManufAmount = this.PlayerWest.AmountOfType(CardType.Manufactured);
                    int thisManufAmount = this.AmountOfType(CardType.Manufactured);
                    if (Resource.Loom <= this.Board.Production &&
                        this.Board.Production <= Resource.Papyrus)
                        thisManufAmount++;
                    if (Resource.Loom <= this.PlayerWest.Board.Production &&
                        this.Board.Production <= Resource.Papyrus)
                        westManufAmount++;
                    if (Resource.Loom <= this.PlayerEast.Board.Production &&
                        this.Board.Production <= Resource.Papyrus)
                        eastManufAmount++;

                    if (thisManufAmount <= 1 &&
                        (westManufAmount >= 1 || eastManufAmount >= 1))
                        score += 2 + westManufAmount + eastManufAmount;     // slightly better than any war or science and better than resources 
                }
                else if (card.Type == CardType.Materials)
                {
                    score = ScoreAfterPlayingCard(1, null);
                    double tradingWeight = 0;

                    // check if enough resources to build wonder before resource card
                    int neededWood, neededStone, neededClay, neededOre, 
                        producedWoodThis, producedWoodWest, producedWoodEast, 
                        producedStoneThis, producedStoneWest, producedStoneEast,
                        producedClayThis, producedClayWest, producedClayEast, 
                        producedOreThis, producedOreWest, producedOreEast;
                    {
                        neededWood = this.Board.HighestCosts[Resource.Wood];
                        neededStone = this.Board.HighestCosts[Resource.Stone];
                        neededClay = this.Board.HighestCosts[Resource.Clay];
                        neededOre = this.Board.HighestCosts[Resource.Ore];

                        producedWoodThis = this.CountProducible(Resource.Wood, neededWood, false);
                        producedWoodWest = this.PlayerWest.CountProducible(Resource.Wood, neededWood, true);
                        producedWoodEast = this.PlayerEast.CountProducible(Resource.Wood, neededWood, true);
                        neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                        producedStoneThis = this.CountProducible(Resource.Stone, neededStone, false);
                        producedStoneWest = this.PlayerWest.CountProducible(Resource.Stone, neededStone, true);
                        producedStoneEast = this.PlayerEast.CountProducible(Resource.Stone, neededStone, true);
                        neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                        producedClayThis = this.CountProducible(Resource.Clay, neededClay, false);
                        producedClayWest = this.PlayerWest.CountProducible(Resource.Clay, neededClay, true);
                        producedClayEast = this.PlayerEast.CountProducible(Resource.Clay, neededClay, true);
                        neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                        producedOreThis = this.CountProducible(Resource.Ore, neededOre, false);
                        producedOreWest = this.PlayerWest.CountProducible(Resource.Ore, neededOre, true);
                        producedOreEast = this.PlayerEast.CountProducible(Resource.Ore, neededOre, true);
                        neededOre = neededOre - producedOreThis - producedOreWest - producedOreEast;
                    }

                    int eastRawAmount = this.PlayerEast.AmountOfType(CardType.Materials);
                    if (this.PlayerEast.Board.Production <= Resource.Stone)
                        eastRawAmount++;
                    int westRawAmount = this.PlayerWest.AmountOfType(CardType.Materials);
                    if (this.PlayerWest.Board.Production <= Resource.Stone)
                        westRawAmount++;
                    if ((this.RawCheapEast && eastRawAmount >= 2) ||
                        (this.RawCheapWest && westRawAmount >= 2) || 
                        (this.WonderRawCheap && 
                            (eastRawAmount >= 2 || westRawAmount >= 2)))
                        tradingWeight = 2;

                    if (neededWood > 0 ||
                        neededStone > 0 ||
                        neededClay > 0 ||
                        neededOre > 0)
                    {
                        if (card.Id == CardId.LumberYard)
                        {
                            neededWood -= 1;
                            if (this.Board.HighestCosts[Resource.Wood] != 0 && neededWood > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (this.Board.HighestCosts[Resource.Wood] != 0 && 
                                (producedWoodThis + producedWoodWest + producedWoodEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.1;    // slightly worse than multichoice 
                            }
                        }
                        else if (card.Id == CardId.StonePit)
                        {
                            neededStone -= 1;
                            if (this.Board.HighestCosts[Resource.Stone] != 0 && neededStone > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (this.Board.HighestCosts[Resource.Stone] != 0 && 
                                (producedStoneThis + producedStoneWest + producedStoneEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.1;    // slightly worse than multichoice 
                            }
                        }
                        else if (card.Id == CardId.ClayPool)
                        {
                            neededClay -= 1;
                            if (this.Board.HighestCosts[Resource.Clay] != 0 && neededClay > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (this.Board.HighestCosts[Resource.Clay] != 0 && 
                                (producedClayThis + producedClayWest + producedClayEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.1;    // slightly worse than multichoice 
                            }
                        }
                        else if (card.Id == CardId.OreVein)
                        {
                            neededOre -= 1;
                            if (this.Board.HighestCosts[Resource.Ore] != 0 && neededOre > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (this.Board.HighestCosts[Resource.Ore] != 0 && 
                                (producedOreThis + producedOreWest + producedOreEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.1;    // slightly worse than multichoice 
                            }
                        }
                        /*else if (card.Id == CardId.TreeFarm || card.Id == CardId.TimberYard || card.Id == CardId.ForestCave)
                        {
                            neededWood -= 1;
                            if (this.Board.HighestCosts[Resource.Wood] != 0 && neededWood - 1 > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.3;     // slightly worse than manufactured goods
                            }
                            else if (this.Board.HighestCosts[Resource.Wood] != 0 &&
                                (producedWoodThis + producedWoodWest + producedWoodEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.3;    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.TimberYard || card.Id == CardId.Mine)
                        {
                            neededStone -= 1;
                            if (this.Board.HighestCosts[Resource.Stone] != 0 && neededStone - 1 > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.3;     // slightly worse than manufactured goods
                            }
                            else if (this.Board.HighestCosts[Resource.Stone] != 0 &&
                                (producedStoneThis + producedStoneWest + producedStoneEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.3;    // slightly better than single resource 
                            }
                        }*/
                        else if (card.Id == CardId.Excavation)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (this.Board.HighestCosts[Resource.Stone] != 0)
                                materialWeight++;
                            if (this.Board.HighestCosts[Resource.Clay] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((this.Board.HighestCosts[Resource.Stone] != 0 &&
                                (producedStoneThis + producedStoneWest + producedStoneEast) <= 1) ||
                                (this.Board.HighestCosts[Resource.Clay] != 0 &&
                                (producedClayThis + producedClayWest + producedClayEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.ClayPit)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (this.Board.HighestCosts[Resource.Clay] != 0)
                                materialWeight++;
                            if (this.Board.HighestCosts[Resource.Ore] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((this.Board.HighestCosts[Resource.Clay] != 0 &&
                                (producedClayThis + producedClayWest + producedClayEast) <= 1) ||
                                (this.Board.HighestCosts[Resource.Ore] != 0 &&
                                (producedOreThis + producedOreWest + producedOreEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.TreeFarm)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (this.Board.HighestCosts[Resource.Clay] != 0)
                                materialWeight++;
                            if (this.Board.HighestCosts[Resource.Wood] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((this.Board.HighestCosts[Resource.Clay] != 0 &&
                                (producedClayThis + producedClayWest + producedClayEast) <= 1) ||
                                (this.Board.HighestCosts[Resource.Wood] != 0 &&
                                (producedWoodThis + producedWoodWest + producedWoodEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.TimberYard)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (this.Board.HighestCosts[Resource.Stone] != 0)
                                materialWeight++;
                            if (this.Board.HighestCosts[Resource.Wood] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((this.Board.HighestCosts[Resource.Stone] != 0 &&
                                (producedStoneThis + producedStoneWest + producedStoneEast) <= 1) ||
                                (this.Board.HighestCosts[Resource.Wood] != 0 &&
                                (producedWoodThis + producedWoodWest + producedWoodEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.ForestCave)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (this.Board.HighestCosts[Resource.Ore] != 0)
                                materialWeight++;
                            if (this.Board.HighestCosts[Resource.Wood] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((this.Board.HighestCosts[Resource.Ore] != 0 &&
                                (producedOreThis + producedOreWest + producedOreEast) <= 1) ||
                                (this.Board.HighestCosts[Resource.Wood] != 0 &&
                                (producedWoodThis + producedWoodWest + producedWoodEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.Mine)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (this.Board.HighestCosts[Resource.Ore] != 0)
                                materialWeight++;
                            if (this.Board.HighestCosts[Resource.Stone] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((this.Board.HighestCosts[Resource.Ore] != 0 &&
                                (producedOreThis + producedOreWest + producedOreEast) <= 1) ||
                                (this.Board.HighestCosts[Resource.Stone] != 0 &&
                                (producedStoneThis + producedStoneWest + producedStoneEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        /*else if (card.Id == CardId.ClayPit || card.Id == CardId.ForestCave || card.Id == CardId.Mine)
                        {
                            neededOre -= 1;
                            if (this.Board.HighestCosts[Resource.Ore] != 0 && neededOre - 1 > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (this.Board.HighestCosts[Resource.Ore] != 0 &&
                                (producedOreThis + producedOreWest + producedOreEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.3;    // slightly better than single resource 
                            }
                        }*/
                    }
                }
                else if (card.Type == CardType.Manufactured)
                {
                    score = ScoreAfterPlayingCard(1, null);

                    // get amounts of potencially producable manufectured goods resources
                    int loomAmount = this.Resources[Resource.Loom] +
                        this.PlayerEast.Resources[Resource.Loom] +
                        this.PlayerWest.Resources[Resource.Loom];

                    int glassAmount = this.Resources[Resource.Glass] +
                        this.PlayerEast.Resources[Resource.Glass] +
                        this.PlayerWest.Resources[Resource.Glass];

                    int papyrusAmount = this.Resources[Resource.Papyrus] +
                        this.PlayerEast.Resources[Resource.Papyrus] +
                        this.PlayerWest.Resources[Resource.Papyrus];

                    // choose extra manuf for first non 0 wonder cost and try then
                    if (this.Board.HighestCosts[Resource.Loom] != 0 && this.Board.HighestCosts[Resource.Loom] > loomAmount && this.HasExtraManuf())
                        loomAmount++;
                    else if (this.Board.HighestCosts[Resource.Glass] != 0 && this.Board.HighestCosts[Resource.Glass] > glassAmount && this.HasExtraManuf())
                        glassAmount++;
                    else if (this.Board.HighestCosts[Resource.Papyrus] != 0 && this.Board.HighestCosts[Resource.Papyrus] > papyrusAmount && this.HasExtraManuf())
                        papyrusAmount++;

                    if (card.Name.Equals("Loom"))
                    {
                        if ((this.Board.HighestCosts[Resource.Loom]) > loomAmount)
                        {
                            score += 2.5;     // slightly worse than double raw resource when its needed for wonder building
                        }
                    }
                    else if (card.Name.Equals("Glassworks"))
                    {
                        if (this.Board.HighestCosts[Resource.Glass] > glassAmount)
                        {
                            score += 2.5;     // slightly worse than double raw resource when its needed for wonder building
                        }
                    }
                    else if (card.Name.Equals("Press"))
                    {
                        if (this.Board.HighestCosts[Resource.Papyrus] > papyrusAmount)
                        {
                            score += 2.5;     // slightly worse than double raw resource when its needed for wonder building
                        }
                    }
                }
                else if (card.Type == CardType.Scientific)
                {
                    score = ScoreAfterPlayingCard(1, card);
                    if (this.AmountOfType(CardType.Scientific) < 1 &&
                        this.AmountOfType(CardType.Materials) > 1)
                        score += 4;
                    else
                        score += 2.3;
                }
                else if (card.Type == CardType.Civilian)
                {
                    score = ScoreAfterPlayingCard(1, card);
                    score -= 0.7;
                }
                else
                    score = ScoreAfterPlayingCard(1, card);

                if (score > maxScore)
                {
                    bestCard = card;
                    maxScore = score;
                }
                else if (!isNeighbor && score == maxScore)
                {
                    // if cards score is equal to max score then choose a card that would be most usefull for neighbor in next turn
                    Player nextNeighbor = this.PlayerWest;
                    List<Card> nextNeighborPlayableCards = nextNeighbor.GetPlayableCards(cardsList);
                    Card? betterBestCard = nextNeighbor.Age1BuildHeuristic(nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            return bestCard;
        }

        public int ScoreAfterPlayingCard(int era, Card? card)
        {
            int score = 0;

            // backup relevant player data before scoring
            //          Player related:  VictoryTokens, DefeatTokens, ResourcesDict, PlayedCards, VictoryPoints,
            //          Card related:    ExtraScience and the trading flags (a trading post or the Marketplace
            //                           tried from the discard pile must not give the discount for free)
            //
            int victoryTokensBckp = this.VictoryTokens;
            int defeatTokensBckp = this.DefeatTokens;
            int victoryPointsBckp = this.VictoryPoints;
            Dictionary<Resource, int> resourcesBckp = this.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            List<Card> playedCardsBckp = this.PlayedCards.Select(e => e).ToList();
            bool extraScienceBckp = this.ExtraScience;
            bool rawCheapEastBckp = this.RawCheapEast, rawCheapWestBckp = this.RawCheapWest, manufCheapBckp = this.ManufCheap;

            // play card
            if (card != null)
            {
                // add before applying the effect, like BuildStructure (the Lighthouse counts itself)
                this.PlayedCards.Add(card);
                this.ApplyCardEffect(card);
            }

            // do battle and copy guild
            this.Battle(era);
            this.CopyGuild();
            // get score
            score = this.CalculateScore();

            // revert player changes
            this.VictoryTokens = victoryTokensBckp;
            this.DefeatTokens = defeatTokensBckp;
            this.VictoryPoints = victoryPointsBckp;
            this.Resources = resourcesBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            this.PlayedCards = playedCardsBckp.Select(e => e).ToList();
            this.ExtraScience = extraScienceBckp;
            this.RawCheapEast = rawCheapEastBckp;
            this.RawCheapWest = rawCheapWestBckp;
            this.ManufCheap = manufCheapBckp;

            resourcesBckp.Clear();
            playedCardsBckp.Clear();

            return score;
        }
    }
}
