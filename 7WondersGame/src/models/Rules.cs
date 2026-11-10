using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGame.src.models
{
    /// <summary>
    /// The rules, as static functions over <see cref="PlayerState"/> (E5): checking, pricing and making a move,
    /// card effects, and (in Rules.Scoring.cs) battles and scoring. Extension methods, so they read as
    /// <c>player.PlanMove(...)</c>. The turn order is in <see cref="Game"/>.
    /// </summary>
    public static partial class Rules
    {
        // =========== Wonder ===========
        /// <summary>Is there a next wonder stage, and can the player pay for it?</summary>
        public static bool CanBuildWonder(this PlayerState p) =>
            p.Board.NextStageCost is { } cost && p.PlanPayment(cost) != null;

        /// <summary>
        /// Build the next wonder stage with a card from a provided cards list: pay for it, bury the card, apply the stage.
        /// </summary>
        /// <returns>true if the stage was built; otherwise false (nothing changes)</returns>
        public static bool BuildWonder(this PlayerState p, Card card, List<Card> cardsList) => p.Resolve(p.PlanWonder(card, cardsList));

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
        public static List<Card> GetPlayableCards(this PlayerState p, List<Card> cardList, bool playAllFree = false)
        {
            List<Card> playableCards = new List<Card>();
            foreach (Card card in cardList)
            {
                // the same card can't be built twice
                if (p.HasPlayedCard(card))
                    continue;

                if (playAllFree || p.CanChainBuildCard(card) || p.PlanPayment(card.Cost) != null)
                    playableCards.Add(card);
            }
            return playableCards;
        }

        /// <summary>
        /// Play a card from a provided cards list: pay for it (unless <paramref name="free_card"/> or chained), move it
        /// into the city and apply its effect.
        /// </summary>
        /// <returns>true if the card was built; otherwise false (nothing changes)</returns>
        public static bool BuildStructure(this PlayerState p, Card card, List<Card> cards, bool free_card) =>
            p.Resolve(p.PlanBuild(ActionType.Build, card, cards, free_card));

        // =========== Moves: plan, place, settle (R5) ===========
        /// <summary>
        /// Step 1: checks a move against the current state and works out what it costs, changing nothing.
        /// Null if the rules don't allow it. <paramref name="source"/> is where the card comes from.
        /// </summary>
        public static PlannedMove? PlanMove(this PlayerState p, ActionType action, Card card, List<Card> source) => action switch
        {
            ActionType.Build => p.PlanBuild(action, card, source, free: false),
            // Olympia A stage 2: one free card per era
            ActionType.BuildFreeOnce => p.Board.Id == WonderId.OlympiaA && p.Board.Stage >= 2 && p.FreeCardOnce
                ? p.PlanBuild(action, card, source, free: true) : null,
            ActionType.BuildWonder => p.PlanWonder(card, source),
            ActionType.Discard => source.Contains(card) ? new PlannedMove(p, action, card, source, PaymentPlan.Free, 0) : null,
            _ => null,
        };

        /// <summary>Step 1 for Halikarnassos: a free build from the discard pile while <see cref="PlayerState.DiscardFree"/> is on.</summary>
        public static PlannedMove? PlanDiscardPileBuild(this PlayerState p, Card card, List<Card> discardPile)
        {
            bool halikarnassos = p.Board.Id == WonderId.HalikarnassosA && p.Board.Stage == 2 ||
                                 p.Board.Id == WonderId.HalikarnassosB && p.Board.Stage >= 1;
            return halikarnassos && p.DiscardFree && p.PlanBuild(ActionType.Build, card, discardPile, free: true) is { } move
                ? move with { FromDiscardPile = true }
                : null;
        }

        private static PlannedMove? PlanBuild(this PlayerState p, ActionType action, Card card, List<Card> source, bool free)
        {
            if (p.HasPlayedCard(card) || !source.Contains(card))
                return null;
            if (free || p.CanChainBuildCard(card))
                return new PlannedMove(p, action, card, source, PaymentPlan.Free, 0);
            if (p.PlanPayment(card.Cost) is { } plan)
                return new PlannedMove(p, action, card, source, plan, card.Cost.TryGetValue(Resource.Coins, out int coins) ? coins : 0);

            return null;
        }

        private static PlannedMove? PlanWonder(this PlayerState p, Card card, List<Card> source)
        {
            if (p.Board.NextStageCost is { } cost && p.PlanPayment(cost) is { } plan && source.Contains(card))
                return new PlannedMove(p, ActionType.BuildWonder, card, source, plan, 0);
            return null;
        }

        /// <summary>
        /// Step 2: the player pays their own coins (to the neighbours and the bank) and the card leaves where it came
        /// from. A built card goes into the city. Nobody receives anything yet.
        /// </summary>
        public static void Place(this PlayerState p, PlannedMove move)
        {
            p.Resources[Resource.Coins] -= move.Payment.Total + move.CoinCost;
            move.Source.Remove(move.Card);
            // The card is in the city before its effect applies, so the Lighthouse counts itself.
            // Sources: https://rules.dized.com/7-wonders/rule/question/19075b63-591e-4e99-8dfc-5fa34b726973,
            //          https://rules.dized.com/7-wonders/rule/symbols/third-age-cards
            if (move.Action is ActionType.Build or ActionType.BuildFreeOnce)
                p.PlayedCards.Add(move.Card);
        }

        /// <summary>
        /// Step 3: the neighbours receive their trade coins and the move takes effect: the card's effect, the wonder
        /// stage, or 3 coins for a discard. A discarded card goes to the discard pile in <see cref="Game.ResolveMoves"/>.
        /// </summary>
        public static void Settle(this PlayerState p, PlannedMove move)
        {
            p.PlayerWest.Resources[Resource.Coins] += move.Payment.ToWest;
            p.PlayerEast.Resources[Resource.Coins] += move.Payment.ToEast;
            switch (move.Action)
            {
                case ActionType.Build:
                case ActionType.BuildFreeOnce:
                    p.ApplyCardEffect(move.Card);
                    // used up: Olympia A's is re-armed at the start of the next era
                    if (move.Action == ActionType.BuildFreeOnce)
                        p.FreeCardOnce = false;
                    if (move.FromDiscardPile)
                        p.DiscardFree = false;
                    break;
                case ActionType.BuildWonder:
                    p.Board.AddStage(p);
                    break;
                case ActionType.Discard:
                    p.Resources[Resource.Coins] += 3;
                    break;
            }
        }

        /// <summary>One player's move on its own (outside a normal turn, and in tests): plan, place, settle.</summary>
        private static bool Resolve(this PlayerState p, PlannedMove? move)
        {
            if (move == null)
                return false;
            p.Place(move);
            p.Settle(move);
            return true;
        }

        /// <summary>A card's effect when it is built: production, coins, shields, science or a trading flag.</summary>
        public static void ApplyCardEffect(this PlayerState p, Card card)
        {
            CardId cardId = card.Id;
            switch (card.Type)
            {
                case CardType.Materials:
                    if (cardId == CardId.LumberYard)
                    {
                        p.Resources[Resource.Wood] += 1;
                    }
                    else if (cardId == CardId.StonePit)
                    {
                        p.Resources[Resource.Stone] += 1;
                    }
                    else if (cardId == CardId.ClayPool)
                    {
                        p.Resources[Resource.Clay] += 1;
                    }
                    else if (cardId == CardId.OreVein)
                    {
                        p.Resources[Resource.Ore] += 1;
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
                        p.Resources[Resource.Wood] += 2;
                    }
                    else if (cardId == CardId.Quarry)
                    {
                        p.Resources[Resource.Stone] += 2;
                    }
                    else if (cardId == CardId.Brickyard)
                    {
                        p.Resources[Resource.Clay] += 2;
                    }
                    else if (cardId == CardId.Foundry)
                    {
                        p.Resources[Resource.Ore] += 2;
                    }
                    break;

                case CardType.Manufactured:
                    if (cardId == CardId.Loom || cardId == CardId.Loom2)
                    {
                        p.Resources[Resource.Loom]++;
                    }
                    else if (cardId == CardId.Glassworks || cardId == CardId.Glassworks2)
                    {
                        p.Resources[Resource.Glass]++;
                    }
                    else if (cardId == CardId.Press || cardId == CardId.Press2)
                    {
                        p.Resources[Resource.Papyrus]++;
                    }
                    break;

                case CardType.Commercial:
                    if (cardId == CardId.Tavern)
                    {
                        p.Resources[Resource.Coins] += 5;
                    }
                    else if (cardId == CardId.EastTradingPost)
                    {
                        p.RawCheapEast = true;
                    }
                    else if (cardId == CardId.WestTradingPost)
                    {
                        p.RawCheapWest = true;
                    }
                    else if (cardId == CardId.Marketplace)
                    {
                        p.ManufCheap = true;
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
                        int brownCards = p.PlayerEast.AmountOfType(CardType.Materials)
                                        + p.PlayerWest.AmountOfType(CardType.Materials)
                                        + p.AmountOfType(CardType.Materials);
                        p.Resources[Resource.Coins] += 1 * brownCards;
                    }
                    else if (cardId == CardId.Bazar)
                    {
                        int greyCards = p.PlayerEast.AmountOfType(CardType.Manufactured)
                                        + p.PlayerWest.AmountOfType(CardType.Manufactured)
                                        + p.AmountOfType(CardType.Manufactured);
                        p.Resources[Resource.Coins] += 2 * greyCards;
                    }
                    else if (cardId == CardId.Haven)
                    {
                        int brownCards = p.AmountOfType(CardType.Materials);
                        p.Resources[Resource.Coins] += 1 * brownCards;
                    }
                    else if (cardId == CardId.Lighthouse)
                    {
                        int yellowCards = p.AmountOfType(CardType.Commercial);
                        p.Resources[Resource.Coins] += 1 * yellowCards;
                    }
                    else if (cardId == CardId.ChamberOfCommerce)
                    {
                        int greyCards = p.AmountOfType(CardType.Manufactured);
                        p.Resources[Resource.Coins] += 2 * greyCards;
                    }
                    else if (cardId == CardId.Arena)
                    {
                        int stages = p.Board.Stage;
                        p.Resources[Resource.Coins] += 3 * stages;
                    }
                    break;

                case CardType.Military:
                    if (cardId == CardId.Stockade || cardId == CardId.Barracks || cardId == CardId.GuardTower)
                    {
                        p.Resources[Resource.Shields] += 1;
                    }
                    else if (cardId == CardId.Walls || cardId == CardId.TrainingGround ||
                               cardId == CardId.Stables || cardId == CardId.ArcheryRange)
                    {
                        p.Resources[Resource.Shields] += 2;
                    }
                    else if (cardId == CardId.Fortifications || cardId == CardId.Circus ||
                               cardId == CardId.Arsenal || cardId == CardId.SiegeWorkshop)
                    {
                        p.Resources[Resource.Shields] += 3;
                    }
                    break;

                case CardType.Scientific:
                    if (cardId == CardId.Workshop || cardId == CardId.Laboratory ||
                        cardId == CardId.Observatory || cardId == CardId.Study)
                    {
                        p.Resources[Resource.Gear]++;
                    }
                    else if (cardId == CardId.Apothecary || cardId == CardId.Dispensary ||
                        cardId == CardId.Lodge || cardId == CardId.Academy)
                    {
                        p.Resources[Resource.Compass]++;
                    }
                    else if (cardId == CardId.Scriptorium || cardId == CardId.Library ||
                        cardId == CardId.School || cardId == CardId.University)
                    {
                        p.Resources[Resource.Tablet]++;
                    }
                    break;

                case CardType.Guild:
                    if (cardId == CardId.Scientists)
                    {
                        p.ExtraScience = true;
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>The no-duplicates rule: true if the city already has a card with this name.</summary>
        private static bool HasPlayedCard(this PlayerState p, Card card)
        {
            foreach (Card playedCard in p.PlayedCards)
                if (playedCard.SameNameAs(card))
                    return true;

            return false;
        }

        private static bool CanChainBuildCard(this PlayerState p, Card card)
        {
            CardId freeWithId = card.FreeWithId;

            if (freeWithId != CardId.None)
            {
                // Treating exception:
                // Forum can be constructed for free if the player has either the East Trading Post
                // or the West Trading Post (raw_cheap_east or raw_cheap_west).
                if (freeWithId == CardId.AnyTradingPost && (p.RawCheapEast || p.RawCheapWest))
                    return true;

                // General case:
                foreach (Card playedCard in p.PlayedCards)
                {
                    if (playedCard.Id == freeWithId)
                        return true;
                }
            }

            return false;
        }

        public static int AmountOfType(this PlayerState p, CardType cardType)
        {
            int amount = 0;
            foreach (Card card in p.PlayedCards)
                if (card.Type == cardType)
                    amount++;

            return amount;
        }

        /// <summary>
        /// Discard a card from HandCards and gain 3 coins
        /// </summary>
        public static void Discard(this PlayerState p, Card card)
        {
            if (!p.Resolve(p.PlanMove(ActionType.Discard, card, p.HandCards)))
                throw new InvalidOperationException($"Seat {p.Id} can't discard {card.Name}: it isn't in their hand.");
        }

        // =========== Wonder effects ===========
        /// <summary>
        /// If DiscardFree effect is active, builds a card from the discarded cards list passed into the function for free.
        /// </summary>
        /// <returns>true if the card was built; otherwise false (nothing changes)</returns>
        public static bool BuildDiscardFree(this PlayerState p, Card card, List<Card> discardedCards) => p.Resolve(p.PlanDiscardPileBuild(card, discardedCards));

        /// <summary>
        /// If FreeCardOnce Olympia A stage 2 effect is active, builds a card from the cards list passed into the function
        /// for free. Works once per era.
        /// </summary>
        public static bool BuildHandFree(this PlayerState p, Card card, List<Card> cardList) => p.Resolve(p.PlanMove(ActionType.BuildFreeOnce, card, cardList));

        // =========== Paying ===========
        /// <summary>The cheapest way to pay <paramref name="cost"/>, or null if the player can't (see <see cref="PaymentPlanner"/>).</summary>
        public static PaymentPlan? PlanPayment(this PlayerState p, IReadOnlyDictionary<Resource, int> cost) => PaymentPlanner.Plan(p, cost);

        /// <summary>Pays the neighbours what the plan says, all at once (a turn pays in <see cref="Place"/> and <see cref="Settle"/>).</summary>
        public static void Pay(this PlayerState p, PaymentPlan plan)
        {
            p.Resources[Resource.Coins] -= plan.Total;
            p.PlayerWest.Resources[Resource.Coins] += plan.ToWest;
            p.PlayerEast.Resources[Resource.Coins] += plan.ToEast;
        }

        /// <summary>Alexandria A stage 2 / B stage 1: one raw resource of the player's choice each turn.</summary>
        internal static bool HasExtraRaw(this PlayerState p) =>
            p.Board.Id == WonderId.AlexandriaA && p.Board.Stage >= 2 || p.Board.Id == WonderId.AlexandriaB && p.Board.Stage >= 1;

        /// <summary>Alexandria B stage 2: one manufactured good of the player's choice each turn.</summary>
        internal static bool HasExtraManuf(this PlayerState p) => p.Board.Id == WonderId.AlexandriaB && p.Board.Stage >= 2;
    }
}
