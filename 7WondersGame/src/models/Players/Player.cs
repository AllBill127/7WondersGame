using _7WondersGame.src.models.Wonders;
using Microsoft.VisualBasic;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // flags used to check multichoice materials only once
        public Dictionary<string, int> UsedOnDemandResource = new Dictionary<string, int>()
        {
            { "UsedTreeFarm", -1},
            { "UsedForestCave", -1},
            { "UsedTimberYard", -1},
            { "UsedExcavation", -1},
            { "UsedMine", -1 },
            { "UsedClayPit", -1},
            { "UsedForum", -1},
            { "UsedCaravansery", -1},
            { "UsedRawExtra", -1},
            { "UsedManufExtra", -1},
        };

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


        // =========== Build related functions ===========
        // =========== Wonder related ===========
        public bool CheckCanBuildWonder()
        {
            Dictionary<Resource, int> resourcesEastBckp = PlayerEast.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<Resource, int> resourcesWestBckp = PlayerWest.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<Resource, int> resourcesBckp = Resources.ToDictionary(entry => entry.Key, entry => entry.Value);

            // check if possible to build wonder stage
            CanBuildWonder = Board.CanAddStage(this);

            Resources = resourcesBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            PlayerEast.Resources = resourcesEastBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            PlayerWest.Resources = resourcesWestBckp.ToDictionary(entry => entry.Key, entry => entry.Value);

            ResetUsed();
            PlayerEast.ResetUsed();
            PlayerWest.ResetUsed();

            return CanBuildWonder;
        }

        /// <summary>
        /// Build wonder with a card from a provided cards list.
        /// </summary>
        /// <param name="card"></param>
        /// <param name="cardsList"></param>
        /// <returns></returns>
        public bool BuildWonder(Card card, List<Card> cardsList)
        {
            // Backup resource dicts in case building wonder fails
            Dictionary<Resource, int> resourcesEastBckp = PlayerEast.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<Resource, int> resourcesWestBckp = PlayerWest.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<Resource, int> resourcesBckp = Resources.ToDictionary(entry => entry.Key, entry => entry.Value);

            if (CanBuildWonder)
            {
                bool buildSuccess = Board.AddStage(this);
                if (buildSuccess)
                {
                    int i;
                    for (i = 0; i < cardsList.Count; i++)
                    {
                        if (cardsList[i].Equals(card))
                            break;
                    }

                    cardsList.RemoveAt(i);

                    // Reset resources and usedOnDemandResource dict when all resources needed were produced and wonder stage was added successfully
                    ResetUsed();
                    PlayerEast.ResetUsed();
                    PlayerWest.ResetUsed();

                    Log.Debug("SUCCESSFULLY built {WonderName} stage {stage} by Player {PlayerId}", Board.Name, Board.Stage, Id);
                    //CanBuildWonder = false;
                    return true;
                }
                else
                    Log.Debug("FAILED to build {WonderName} stage {stage} by Player {PlayerId}\n" +
                        " -> CanBuildWonder: {CanBuildWonder} but buildStage: {buildSuccess}",
                        Board.Name, Board.Stage+1, Id, CanBuildWonder, buildSuccess);
            }
            else
                Log.Debug("FAILED to build {WonderName} stage {stage} by Player {PlayerId}\n" +
                    " -> CanBuildWonder: {CanBuildWonder}",
                    Board.Name, Board.Stage+1, Id, CanBuildWonder);

            // Reset usedOnDemandResource dict when adding wonder stage fails
            Resources = resourcesBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            ResetUsed();
            PlayerEast.Resources = resourcesEastBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            PlayerEast.ResetUsed();
            PlayerWest.Resources = resourcesWestBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            PlayerWest.ResetUsed();

            Log.Error("SHOULD NOT HAPPEN");

            return false;
        }

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

            // Backup resources to revert changes after checking if needed resources can be succeffully bought 
            Dictionary<Resource, int> resourcesEastBckp = PlayerEast.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<Resource, int> resourcesWestBckp = PlayerWest.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<Resource, int> resourcesBckp = Resources.ToDictionary(entry => entry.Key, entry => entry.Value);

            foreach (Card card in cardList)
            {
                bool playable = true;

                // Skip this card if player has already played it
                if (HasPlayedCard(card))
                    continue;

                // Check if player can play all cards in list for free flag is on
                if (!playAllFree)
                {
                    // Check if player can chain build this card
                    if (!CanChainBuildCard(card))
                    {
                        Dictionary<Resource, int> resourcesNeeded = card.Cost;
                        foreach (var resourceKvp in resourcesNeeded)
                        {
                            int quantNeeded = resourceKvp.Value;
                            if (quantNeeded > 0)
                            {
                                bool couldProduce = ProduceResource(resourceKvp.Key, quantNeeded);
                                // If it was not possible to produce/buy, revert changes to resource and onDemandResource dicts and return.
                                if (!couldProduce)
                                {
                                    Resources = resourcesBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                                    ResetUsed();
                                    PlayerEast.Resources = resourcesEastBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                                    PlayerEast.ResetUsed();
                                    PlayerWest.Resources = resourcesWestBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                                    PlayerWest.ResetUsed();
                                    Log.Debug("Player {PlayerId} -> CANT BUILD {cardName} -> couldn't produce resources.", Id, card.Name);
                                    playable = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Add card to playable cards list
                if (playable)
                    playableCards.Add(card);

                // Reset resource and useOnDemandResource dicts for the next card evaluation
                Resources = resourcesBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                ResetUsed();
                PlayerEast.Resources = resourcesEastBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                PlayerEast.ResetUsed();
                PlayerWest.Resources = resourcesWestBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                PlayerWest.ResetUsed();
            }

            // Return the playable cards list
            return playableCards;
        }

        /// <summary>
        /// Play a card from a provided cards list.
        /// </summary>
        /// <param name="card"></param>
        /// <param name="cards"></param>
        /// <param name="free_card"></param>
        /// <returns></returns>
        public bool BuildStructure(Card card, List<Card> cards, bool free_card)
        {
            // Returns false if the card has already been played (cannot play the same card twice).
            if (HasPlayedCard(card))
            {
                Log.Debug(" -> FAILURE: Card Already played but was an option in cards_playable.");
                Log.Error("SHOULD NOT HAPPEN -> Trying to build card that has already been build");
                return false;
            }

            // Checks if card is in hand.
            int i = 0;
            for (i = 0; i < cards.Count; i++)
            {
                if (cards[i].Equals(card))
                    break;
            }

            if (i == cards.Count)
            {
                // ERROR: Card played was not found in the vector
                Log.Debug(" -> FAILURE: Card not found in vector.");
                Log.Error("SHOULD NOT HAPPEN -> Trying to build card that is not in provided cards list");
                return false;
            }

            // Check if the card can be played for free, otherwise try to produce the resources needed.
            if (!free_card)
            {
                // Check if player can chain build this card
                if (CanChainBuildCard(card))
                    free_card = true;
                else
                {
                    // Backup resource dicts in case building structure fails
                    Dictionary<Resource, int> resourcesEastBckp = PlayerEast.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
                    Dictionary<Resource, int> resourcesWestBckp = PlayerWest.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
                    Dictionary<Resource, int> resourcesBckp = Resources.ToDictionary(entry => entry.Key, entry => entry.Value);

                    Dictionary<Resource, int> resourcesNeeded = card.Cost;
                    foreach (var resourceKvp in resourcesNeeded)
                    {
                        int quantNeeded = resourceKvp.Value;
                        if (quantNeeded > 0)
                        {
                            bool couldProduce = ProduceResource(resourceKvp.Key, quantNeeded);
                            // If it was not possible to produce/buy, revert changes to resource and onDemandResource dicts and return.
                            if (!couldProduce)
                            {
                                Log.Debug("Player {PlayerId} -> FAILURE -> couldn't produce resources.", Id);
                                Log.Error("SHOULD NOT HAPPEN -> If CanPlayCard() = true, then building must be possible");
                                Resources = resourcesBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                                ResetUsed();
                                PlayerEast.Resources = resourcesEastBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                                PlayerEast.ResetUsed();
                                PlayerWest.Resources = resourcesWestBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
                                PlayerWest.ResetUsed();
                                return false;
                            }
                        }
                    }

                    // Reset usedOnDemandResource dict when all resources needed were produced
                    ResetUsed();
                    PlayerEast.ResetUsed();
                    PlayerWest.ResetUsed();
                }
            }

            // Aplly card effects and calculate Coin costs
            int cost = ApplyCardEffect_GetCost(card);

            // Move played card from card list to players played cards
            cards.RemoveAt(i);
            PlayedCards.Add(card);

            // Pay for the card if necessary
            if (!free_card) Resources[Resource.Coins] -= cost;
            Log.Debug("Player {PlayerId} -> SUCCESSFULLY -> builds {CardName}", Id, card.Name);

            return true;
        }

        protected int ApplyCardEffect_GetCost(Card card)
        {
            int cost = 0;
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
                        // wood or clay (won't be applied here, see ProduceResource)
                        cost = 1;
                    }
                    else if (cardId == CardId.Excavation)
                    {
                        // stone or clay (won't be applied here, see ProduceResource)
                        cost = 1;
                    }
                    else if (cardId == CardId.ClayPit)
                    {
                        // clay or ore (won't be applied here, see ProduceResource)
                        cost = 1;
                    }
                    else if (cardId == CardId.TimberYard)
                    {
                        // stone or wood (won't be applied here, see ProduceResource)
                        cost = 1;
                    }
                    else if (cardId == CardId.ForestCave)
                    {
                        // wood or ore (won't be applied here, see ProduceResource)
                        cost = 1;
                    }
                    else if (cardId == CardId.Mine)
                    {
                        // ore or stone (won't be applied here, see ProduceResource)
                        cost = 1;
                    }
                    else if (cardId == CardId.Sawmill)
                    {
                        Resources[Resource.Wood] += 2;
                        cost = 1;
                    }
                    else if (cardId == CardId.Quarry)
                    {
                        Resources[Resource.Stone] += 2;
                        cost = 1;
                    }
                    else if (cardId == CardId.Brickyard)
                    {
                        Resources[Resource.Clay] += 2;
                        cost = 1;
                    }
                    else if (cardId == CardId.Foundry)
                    {
                        Resources[Resource.Ore] += 2;
                        cost = 1;
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
                        // lomm or glass or papyrus (won't be applied here, see ProduceResource)
                    }
                    else if (cardId == CardId.Caravansery)
                    {
                        // clay or stone or ore or wood (won't be applied here, see ProduceResource)
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

            return cost;
        }

        private bool HasPlayedCard(Card card)
        {
            foreach (Card playedCard in PlayedCards)
                if (playedCard.Equals(card))
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
        /// <param name="card"></param>
        public void Discard(Card card)
        {
            Resources[Resource.Coins] += 3;

            int i;
            for (i = 0; i < HandCards.Count; i++)
            {
                if (HandCards[i].Equals(card))
                    break;
            }
            HandCards.RemoveAt(i);
        }

        // =========== Wonder effect related ===========
        /// <summary>
        /// If DiscardFree effect is active, builds a card from the discarded cards list passed into the function <br/>
        /// - caller is responsible to ensure that the card is playable (No duplicate cards played rule)
        /// </summary>
        /// <param name="card"></param>
        /// <param name="discardedCards"></param>
        /// <returns>
        /// true if building structure was successfull; otherwise false
        /// </returns>
        public bool BuildDiscardFree(Card card, List<Card> discardedCards)
        {
            WonderId id = Board.Id;
            int stage = Board.Stage;

            if (id == WonderId.HalikarnassosA && stage == 2 ||
                id == WonderId.HalikarnassosB && stage >= 1)
            {
                if (DiscardFree)
                {
                    // disable flag after using this effect
                    DiscardFree = false;
                    bool buildSuccess = BuildStructure(card, discardedCards, true);
                    if (buildSuccess)
                        return true;
                    else
                        Log.Error("SHOULD NOT HAPPEN");
                }
            }
            return false;
        }

        /// <summary>
        /// If FreeCardOnce Olympia A stage 2 effect is active, builds a card from the cards list passed into the function <br/>
        /// - should work only once per game era
        /// </summary>
        /// <param name="card"></param>
        /// <param name="cardList"></param>
        /// <returns></returns>
        public bool BuildHandFree(Card card, List<Card> cardList)
        {
            WonderId id = Board.Id;
            int stage = Board.Stage;

            if (id == WonderId.OlympiaA && stage >= 2)
            {
                if (FreeCardOnce)
                {
                    // disable flag so the player won't be able to build for free in the same game era
                    // should be reenabled if applicable when starting the next game era
                    FreeCardOnce = false;
                    bool buildSuccess = BuildStructure(card, cardList, true);
                    if (buildSuccess)
                        return true;
                    else
                        Log.Error("SHOULD NOT HAPPEN");
                }
            }
            return false;
        }



        // =========== Resource related ===========
        // =========== Resource gathering functions ===========
        /// <summary>
        /// Try producing a required quantity of some resource. <br/>
        /// - function calling this method is responsible for reseting used on demand resources and resource maps if neccessery.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="quant"></param>
        /// <returns>
        /// true if able to produce.
        /// </returns>
        public bool ProduceResource(Resource resource, int quant)
        {
            if (quant == 0) return true;

            int missing = quant - GetResourceOnDemand(resource, quant, false);
            if (missing <= 0)
            {
                return true;
            }

            return BuyMissingRessource(resource, missing);
        }

        bool AvailableCard(CardId card_id, Resource resource)
        {
            foreach (Card card in PlayedCards)
            {
                CardId cardId = card.Id;
                // check if card in question has been played by the player
                if (card.Id == card_id)
                {
                    // check if resource has been chosen already
                    if (cardId == CardId.TreeFarm && UsedOnDemandResource["UsedTreeFarm"] == -1)
                    {
                        UsedOnDemandResource["UsedTreeFarm"] = (int)resource;
                        return true;
                    }
                    else if (cardId == CardId.ForestCave && UsedOnDemandResource["UsedForestCave"] == -1)
                    {
                        UsedOnDemandResource["UsedForestCave"] = (int)resource;
                        return true;
                    }
                    else if (cardId == CardId.TimberYard && UsedOnDemandResource["UsedTimberYard"] == -1)
                    {
                        UsedOnDemandResource["UsedTimberYard"] = (int)resource;
                        return true;
                    }
                    else if (cardId == CardId.Excavation && UsedOnDemandResource["UsedExcavation"] == -1)
                    {
                        UsedOnDemandResource["UsedExcavation"] = (int)resource;
                        return true;
                    }
                    else if (cardId == CardId.Mine && UsedOnDemandResource["UsedMine"] == -1)
                    {
                        UsedOnDemandResource["UsedMine"] = (int)resource;
                        return true;
                    }
                    else if (cardId == CardId.ClayPit && UsedOnDemandResource["UsedClayPit"] == -1)
                    {
                        UsedOnDemandResource["UsedClayPit"] = (int)resource;
                        return true;
                    }
                    else if (cardId == CardId.Forum && UsedOnDemandResource["UsedForum"] == -1)
                    {
                        UsedOnDemandResource["UsedForum"] = (int)resource;
                        return true;
                    }
                    else if (cardId == CardId.Caravansery && UsedOnDemandResource["UsedCaravansery"] == -1)
                    {
                        UsedOnDemandResource["UsedCaravansery"] = (int)resource;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Reset changes to on demand resource flags <br/>
        /// - called at the end of a turn, after checking if enough resources and after failing to buy resources.
        /// </summary>
        void ResetUsed()
        {
            UsedOnDemandResource["UsedTreeFarm"] = -1;
            UsedOnDemandResource["UsedForestCave"] = -1;
            UsedOnDemandResource["UsedTimberYard"] = -1;
            UsedOnDemandResource["UsedExcavation"] = -1;
            UsedOnDemandResource["UsedMine"] = -1;
            UsedOnDemandResource["UsedClayPit"] = -1;
            UsedOnDemandResource["UsedForum"] = -1;
            UsedOnDemandResource["UsedCaravansery"] = -1;
            UsedOnDemandResource["UsedRawExtra"] = -1;
            UsedOnDemandResource["UsedManufExtra"] = -1;
        }

        /// <summary>
        /// The player chooses one of the 4 raw resources to receive for free from a wonder at each turn (untradable). <br/>
        /// - called ONCE each turn; part of IncrementOnDemand().
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>
        /// true if able to choose free resource
        /// </returns>
        bool ChooseExtraRaw(Resource resource)
        {
            if (resource == Resource.Wood || resource == Resource.Ore ||
                resource == Resource.Clay || resource == Resource.Stone)
            {
                WonderId id = Board.Id;
                int stage = Board.Stage;
                if (id == WonderId.AlexandriaA && stage >= 2 ||
                    id == WonderId.AlexandriaB && stage >= 1)
                {
                    if (UsedOnDemandResource["UsedRawExtra"] == -1)
                    {
                        UsedOnDemandResource["UsedRawExtra"] = (int)resource;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// The player chooses one of the 3 manufactured resources to receive for free from a wonder at each turn (untradeable). <br/>
        /// - called ONCE each turn; part of IncrementOnDemand().
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>
        /// true if able to choose free resource
        /// </returns>
        bool ChooseExtraManuf(Resource resource)
        {
            if (resource == Resource.Loom || resource == Resource.Glass || resource == Resource.Papyrus)
            {
                if (Board.Id == WonderId.AlexandriaB && Board.Stage >= 2)
                {
                    if (UsedOnDemandResource["UsedManufExtra"] == -1)
                    {
                        UsedOnDemandResource["UsedManufExtra"] = (int)resource;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Try gathering needed quantity of resource from players played cards. <br/>
        /// Chooses resources from available on demand resource cards but no more than required.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="needed"></param>
        /// <param name="isNeighbor"></param>
        /// <returns>
        /// quantity of produced resource
        /// </returns>
        int GetResourceOnDemand(Resource resource, int needed, bool isNeighbor)
        {
            // From: https://github.com/dmag-ufsm/Game/blob/master/references/cards.csv
            // Wood                 -> tree_farm  || forest_cave || timber_yard || caravansery (yellow)
            // Clay                 -> tree_farm  || excavation  || clay_pit    || caravansery (yellow)
            // Stone                -> excavation || timber_yard || mine        || caravansery (yellow)
            // Ore                  -> clay_pit   || forest_cave || mine        || caravansery (yellow)
            // Glass, Loom, Papyrus -> forum (yellow)

            int curr_resource = Resources[resource];

            // skip further checks if basic resources are enough
            if (curr_resource >= needed) return needed; // to avoid all the extra checks below if needed

            int missing = needed - curr_resource;
            // choose resources on demand until missing amount is reached or all options are exhausted
            if (resource == Resource.Wood)
            {
                if (missing > 0 && AvailableCard(CardId.TreeFarm, resource))
                {
                    missing--;
                }
                if (missing > 0 && AvailableCard(CardId.ForestCave, resource))
                {
                    missing--;
                }
                if (missing > 0 && AvailableCard(CardId.TimberYard, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && AvailableCard(CardId.Caravansery, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && ChooseExtraRaw(resource))
                {
                    missing--;
                }
            }
            else if (resource == Resource.Clay)
            {
                if (missing > 0 && AvailableCard(CardId.TreeFarm, resource))
                {
                    missing--;
                }
                if (missing > 0 && AvailableCard(CardId.Excavation, resource))
                {
                    missing--;
                }
                if (missing > 0 && AvailableCard(CardId.ClayPit, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && AvailableCard(CardId.Caravansery, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && ChooseExtraRaw(resource))
                {
                    missing--;
                }
            }
            else if (resource == Resource.Stone)
            {
                if (missing > 0 && AvailableCard(CardId.Excavation, resource))
                {
                    missing--;
                }
                if (missing > 0 && AvailableCard(CardId.TimberYard, resource))
                {
                    missing--;
                }
                if (missing > 0 && AvailableCard(CardId.Mine, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && AvailableCard(CardId.Caravansery, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && ChooseExtraRaw(resource))
                {
                    missing--;
                }
            }
            else if (resource == Resource.Ore)
            {
                if (missing > 0 && AvailableCard(CardId.ClayPit, resource))
                {
                    missing--;
                }
                if (missing > 0 && AvailableCard(CardId.ForestCave, resource))
                {
                    missing--;
                }
                if (missing > 0 && AvailableCard(CardId.Mine, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && AvailableCard(CardId.Caravansery, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && ChooseExtraRaw(resource))
                {
                    missing--;
                }
            }
            else if (resource == Resource.Glass || resource == Resource.Loom || resource == Resource.Papyrus)
            {
                if (!isNeighbor && missing > 0 && AvailableCard(CardId.Forum, resource))
                {
                    missing--;
                }
                if (!isNeighbor && missing > 0 && ChooseExtraManuf(resource))
                {
                    missing--;
                }
            }

            return needed - missing; // Returns quantity of produced resource.
        }

        // =========== Commerce functions ===========
        /// <summary>
        /// Buys quantity needed of a resource from a specific neighbor. <br/>
        /// Chooses on demand resources available from neighbor.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="needed"></param>
        /// <param name="cheap_neighbor"></param>
        /// <param name="neighbor"></param>
        /// <returns>
        /// quantity of resource unable to be bought.
        /// </returns>
        int BuyFromNeighbor(Resource resource, int needed, bool cheap_neighbor, Player neighbor)
        {
            bool is_raw = (int)resource <= 3 ? true : false; // Raw materials have index <= 3 in Resource enum

            int produced = neighbor.GetResourceOnDemand(resource, needed, true);

            if (produced > 0)
            {
                int cost;
                if (is_raw && (cheap_neighbor || WonderRawCheap))
                    cost = 1 * produced;
                else if (!is_raw && ManufCheap)
                    cost = 1 * produced;
                else
                    cost = 2 * produced;

                if (Resources[Resource.Coins] >= cost)
                {
                    Resources[Resource.Coins] -= cost;
                    neighbor.Resources[Resource.Coins] += cost;
                }
                else
                    return needed;
            }

            return needed - produced;
        }

        /// <summary>
        /// Buys x quantity of a certain resource from any neighbor. <br/>
        /// - this function is a "step" in BuildStructure.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="needed"></param>
        /// <returns></returns>
        bool BuyMissingRessource(Resource resource, int missing)
        {
            Dictionary<Resource, int> resourcesEastBckp = PlayerEast.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<string, int> usedResourcesEastBckp = PlayerEast.UsedOnDemandResource.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<Resource, int> resourcesWestBckp = PlayerWest.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<string, int> usedResourcesWestBckp = PlayerWest.UsedOnDemandResource.ToDictionary(entry => entry.Key, entry => entry.Value);
            Dictionary<Resource, int> resourcesBckp = Resources.ToDictionary(entry => entry.Key, entry => entry.Value);

            // Player will choose to buy from cheapest neighbor first.
            if (RawCheapWest)
            {
                missing = BuyFromNeighbor(resource, missing, RawCheapWest, PlayerWest);
                if (missing == 0)
                {
                    Log.Debug("Player {PlayerId} bought {resource} from west only!", Id, resource);
                    return true;
                }
                else
                {
                    missing = BuyFromNeighbor(resource, missing, RawCheapEast, PlayerEast);
                    if (missing == 0)
                    {
                        Log.Debug("Player {PlayerId} bought {resource} from east!", Id, resource);
                        return true;
                    }
                }
            }
            else
            {
                missing = BuyFromNeighbor(resource, missing, RawCheapEast, PlayerEast);
                if (missing == 0)
                {
                    Log.Debug("Player {PlayerId} bought {resource} from east only!", Id, resource);
                    return true;
                }
                else
                {
                    missing = BuyFromNeighbor(resource, missing, RawCheapWest, PlayerWest);
                    if (missing == 0)
                    {
                        Log.Debug("Player {PlayerId} bought {resource} from west!", Id, resource);
                        return true;
                    }
                }
            }

            // If buying enough resources fails, undo resource map changes (coins spent/gained and on demand resource flags changed in Neighbors).
            PlayerEast.UsedOnDemandResource = usedResourcesEastBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            PlayerWest.UsedOnDemandResource = usedResourcesWestBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            PlayerEast.Resources = resourcesEastBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            PlayerWest.Resources = resourcesWestBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            Resources = resourcesBckp.ToDictionary(entry => entry.Key, entry => entry.Value);

            Log.Debug("FAILURE -> Player {PlayerId} couldn't buy enough {resource}.", Id, resource);
            return false; // Couldn't buy
        }



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
                //Log.Debug("Player {PlayerId} -> WON battle with Player {EastPlayerId}", Id, PlayerEast.Id);
                VictoryTokens += current_era_value;
            }
            else if (this_shields < PlayerEast.Resources[Resource.Shields])
            {
                //Log.Debug("Player {PlayerId} -> LOST battle with Player {EastPlayerId}", Id, PlayerEast.Id);
                DefeatTokens += 1;
            }
            else
            {
                //Log.Debug("Player {PlayerId} -> DRAW battle with Player {EastPlayerId}", Id, PlayerEast.Id);
            }

            // Battle with west neighbor
            if (this_shields > PlayerWest.Resources[Resource.Shields])
            {
                //Log.Debug("Player {PlayerId} -> WON battle with Player {EastPlayerId}", Id, PlayerWest.Id);
                VictoryTokens += current_era_value;
            }
            else if (this_shields < PlayerWest.Resources[Resource.Shields])
            {
                //Log.Debug("Player {PlayerId} -> LOST battle with Player {EastPlayerId}", Id, PlayerWest.Id);
                DefeatTokens += 1;
            }
            else
            {
                //Log.Debug("Player {PlayerId} -> DRAW battle with Player {EastPlayerId}", Id, PlayerWest.Id);
            }
        }

        /// <summary>
        /// Copies highest VP earning guild from neighbors to a Players PlayedCards list <br/>
        /// - should be called before calculating scores
        /// </summary>
        /// <returns>
        /// true if a guild was copied; otherwise false
        /// </returns>
        public bool CopyGuild()
        {
            WonderId id = Board.Id;
            int stage = Board.Stage;

            // { guild; total VPs it can give to the current player }
            Dictionary<CardId, int> guild_vps = new()
            {
                { CardId.Workers, 0 },
                { CardId.Craftsmens, 0 },
                { CardId.Traders, 0 },
                { CardId.Philosophers, 0 },
                { CardId.Spies, 0 },
                { CardId.Magistrates, 0 },
                { CardId.Shipowners, 0 },
                { CardId.Strategists, 0 },
                { CardId.Scientists, 0 },
                { CardId.Builders, 0 },
            };

            if (id == WonderId.OlympiaB && stage >= 3)
            {
                List<Card> available_guilds = new();
                foreach (Card card in PlayerEast.PlayedCards)
                {
                    if (card.Type == CardType.Guild)
                        available_guilds.Add(card);
                }
                foreach (Card card in PlayerWest.PlayedCards)
                {
                    if (card.Type == CardType.Guild)
                        available_guilds.Add(card);
                }

                // For each guild the neighbors have, calculate the potential VPs it can give
                // to the current player.
                // The guild with the most VPs will be the chosen one to be copied.
                foreach (Card guild in available_guilds)
                {
                    CardId guild_id = guild.Id;
                    switch (guild_id)
                    {
                        case CardId.Workers:
                            guild_vps[CardId.Workers] = PlayerEast.AmountOfType(CardType.Materials) + PlayerWest.AmountOfType(CardType.Materials);
                            break;
                        case CardId.Craftsmens:
                            guild_vps[CardId.Craftsmens] = PlayerEast.AmountOfType(CardType.Manufactured) + PlayerWest.AmountOfType(CardType.Manufactured);
                            break;
                        case CardId.Traders:
                            guild_vps[CardId.Traders] = PlayerEast.AmountOfType(CardType.Commercial) + PlayerWest.AmountOfType(CardType.Commercial);
                            break;
                        case CardId.Philosophers:
                            guild_vps[CardId.Philosophers] = PlayerEast.AmountOfType(CardType.Scientific) + PlayerWest.AmountOfType(CardType.Scientific);
                            break;
                        case CardId.Spies:
                            guild_vps[CardId.Spies] = PlayerEast.AmountOfType(CardType.Military) + PlayerWest.AmountOfType(CardType.Military);
                            break;
                        case CardId.Magistrates:
                            guild_vps[CardId.Magistrates] = PlayerEast.AmountOfType(CardType.Civilian) + PlayerWest.AmountOfType(CardType.Civilian);
                            break;
                        case CardId.Shipowners:
                            guild_vps[CardId.Shipowners] = AmountOfType(CardType.Materials) + AmountOfType(CardType.Manufactured);
                            guild_vps[CardId.Shipowners] += AmountOfType(CardType.Guild);
                            break;
                        case CardId.Strategists:
                            guild_vps[CardId.Strategists] = PlayerEast.DefeatTokens + PlayerWest.DefeatTokens;
                            break;
                        case CardId.Scientists:
                            // Not 100% accurate.
                            guild_vps[CardId.Scientists] = AmountOfType(CardType.Scientific);
                            break;
                        case CardId.Builders:
                            guild_vps[CardId.Builders] = PlayerEast.Board.Stage + PlayerWest.Board.Stage;
                            guild_vps[CardId.Builders] += Board.Stage;
                            break;
                        default:
                            break;
                    }
                }

                KeyValuePair<CardId, int> best_guild = guild_vps.MaxBy(kvp => kvp.Value);

                if (best_guild.Value > 0)
                {
                    foreach (Card guild in available_guilds)
                    {
                        // Pushes the chosen guild to the vector of played cards.
                        if (guild.Id == best_guild.Key)
                        {
                            PlayedCards.Add(guild);
                            Log.Debug("Player {PlayerId} -> SUCCESSFULLY -> copies guild {GuildName}", Id, guild.Name);
                            return true;
                        }
                    }
                }
                Log.Debug("Player {PlayerId} -> FAILED -> no guilds to copy!", Id);
            }

            return false;
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

            // Choose the most advantageous scientific piece for extra piece:
            // First complete the set if possible as it gives 7 + typeAmount^2 - typeAmount.
            // The pieces left over after completing sets are added to type that has most typeAmount.
            // If remaining type sum is > (extra = 2) then adding extra to the most typeAmount gives highest VP
            // NOTE:    not implemented yet. Do testing to find out the need for this. (extra:1 gear:3 compass:2 tablet:1 = 31 (not 28))
            // If remainingTypeSum is > (extra = 1) AND remainingTypeSum % 2 = 1 AND most typeAmount = 3
            //      then 1 set can be completed and it gives more VP than adding to most typeAmount
            int remaining_tablets,
                remaining_compasses,
                remaining_gears,
                extra = sci_extra;
            if (tablet >= compass && tablet >= gear)
            {
                remaining_compasses = tablet - compass;
                remaining_gears = tablet - gear;
                if (remaining_compasses + remaining_gears <= extra)
                {
                    compass += remaining_compasses;
                    gear += remaining_gears;
                    extra -= remaining_compasses + remaining_gears;
                }
                tablet += extra;
            }
            else if (compass >= tablet && compass >= gear)
            {
                remaining_tablets = compass - tablet;
                remaining_gears = compass - gear;
                if (remaining_tablets + remaining_gears <= extra)
                {
                    tablet += remaining_tablets;
                    gear += remaining_gears;
                    extra -= remaining_tablets + remaining_gears;
                }
                compass += extra;
            }
            else if (gear >= tablet && gear >= compass)
            {
                remaining_tablets = gear - tablet;
                remaining_compasses = gear - compass;
                if (remaining_compasses + remaining_tablets <= extra)
                {
                    compass += remaining_compasses;
                    tablet += remaining_tablets;
                    extra -= remaining_compasses + remaining_tablets;
                }
                gear += extra;
            }

            // The smallest value among the three is the amount of completed sets
            int completed_sets = 0;
            if (gear <= tablet && gear <= compass) completed_sets = gear;
            if (tablet <= gear && tablet <= compass) completed_sets = tablet;
            if (compass <= gear && compass <= tablet) completed_sets = compass;

            int points_per_set_completed = 7;
            return gear * gear + tablet * tablet + compass * compass + completed_sets * points_per_set_completed;
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
        /// - game and commandStr are used to evaluate card choice <br/>
        /// - if return is null the move should be skipped!
        /// </summary>
        /// <param name="game"></param>
        /// <param name="commandStr"></param>
        /// <param name="cardsList"></param>
        /// <returns>
        /// chosen Card for the turn or null if cardList was empty
        /// </returns>
        public abstract Card? ChooseMoveCard(Game game, string commandStr, List<Card> cardsList);

        /// <summary>
        /// Choose a move for the turn <br/>
        /// - game is used to evaluate move and card choice
        /// </summary>
        /// <param name="game"></param>
        public abstract void ChooseMoveCommand(Game game);

        // =========== Deterministic heuristics ===========
        public Card? Age3BuildHeuristic(Game game, List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;

            foreach (Card card in cardsList)
            {
                score = ScoreAfterPlayingCard(game, card);

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
                    Card? betterBestCard = nextNeighbor.Age3BuildHeuristic(game, nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            return bestCard;
        }

        public Card? Age2BuildHeuristic(Game game, List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;

            foreach (Card card in cardsList)
            {
                if (card.Id == CardId.Vineyard &&
                    this.Resources[Resource.Coins] <= 3)
                {
                    score = ScoreAfterPlayingCard(game, null);
                    // add score for gaining money with extra weight 
                    int cardAmount = this.AmountOfType(CardType.Materials) + 
                        this.PlayerEast.AmountOfType(CardType.Materials) + 
                        this.PlayerWest.AmountOfType(CardType.Materials);
                    score += 1 * cardAmount * 2;
                }
                else if (card.Id == CardId.Bazar &&
                    this.Resources[Resource.Coins] <= 3)
                {
                    score = ScoreAfterPlayingCard(game, null);
                    // add score for gaining money with extra weight 
                    int cardAmount = this.AmountOfType(CardType.Manufactured) +
                        this.PlayerEast.AmountOfType(CardType.Manufactured) +
                        this.PlayerWest.AmountOfType(CardType.Manufactured);
                    score += 2 * cardAmount * 2;
                }
                else if (card.Id == CardId.Forum)
                {
                    score = ScoreAfterPlayingCard(game, null);
                    Dictionary<string, int> usedResourceBckp = this.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // get amounts of potencially producable manufectured goods resources
                    int loomAmount = this.Resources[Resource.Loom] +
                        this.PlayerEast.Resources[Resource.Loom] +
                        this.PlayerWest.Resources[Resource.Loom];
                    if (loomAmount == 0 && this.ChooseExtraManuf(Resource.Loom))
                        loomAmount++;

                    int glassAmount = this.Resources[Resource.Glass] +
                        this.PlayerEast.Resources[Resource.Glass] +
                        this.PlayerWest.Resources[Resource.Glass];
                    if (glassAmount == 0 && this.ChooseExtraManuf(Resource.Glass))
                        glassAmount++;

                    int papyrusAmount = this.Resources[Resource.Papyrus] +
                        this.PlayerEast.Resources[Resource.Papyrus] +
                        this.PlayerWest.Resources[Resource.Papyrus];
                    if (papyrusAmount == 0 && this.ChooseExtraManuf(Resource.Papyrus))
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

                    // reset usedOnDemandResource map
                    this.UsedOnDemandResource = usedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
                else if (card.Id == CardId.Caravansery)
                {
                    score = ScoreAfterPlayingCard(game, null);
                    Dictionary<string, int> thisUsedResourceBckp = this.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    Dictionary<string, int> westUsedResourceBckp = this.PlayerWest.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    Dictionary<string, int> eastUsedResourceBckp = this.PlayerEast.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // check if enough resources to build wonder before resource card
                    int neededWood = this.Board.HighestCosts[Resource.Wood],
                        neededStone = this.Board.HighestCosts[Resource.Stone],
                        neededClay = this.Board.HighestCosts[Resource.Clay],
                        neededOre = this.Board.HighestCosts[Resource.Ore];

                    int producedWoodThis = this.GetResourceOnDemand(Resource.Wood, neededWood, false);
                    int producedWoodWest = this.PlayerWest.GetResourceOnDemand(Resource.Wood, neededWood, true);
                    int producedWoodEast = this.PlayerEast.GetResourceOnDemand(Resource.Wood, neededWood, true);
                    neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                    // reset usedOnDemandResource map for next resource calculation
                    this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    int producedStoneThis = this.GetResourceOnDemand(Resource.Stone, neededStone, false);
                    int producedStoneWest = this.PlayerWest.GetResourceOnDemand(Resource.Stone, neededStone, true);
                    int producedStoneEast =  this.PlayerEast.GetResourceOnDemand(Resource.Stone, neededStone, true);
                    neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                    // reset usedOnDemandResource map for next resource calculation
                    this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    int producedClayThis = this.GetResourceOnDemand(Resource.Clay, neededClay, false);
                    int producedClayWest = this.PlayerWest.GetResourceOnDemand(Resource.Clay, neededClay, true);
                    int producedClayEast =  this.PlayerEast.GetResourceOnDemand(Resource.Clay, neededClay, true);
                    neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                    // reset usedOnDemandResource map for next resource calculation
                    this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    int producedOreThis = this.GetResourceOnDemand(Resource.Ore, neededOre, false);
                    int producedOreWest = this.PlayerWest.GetResourceOnDemand(Resource.Ore, neededOre, true);
                    int producedOreEast =  this.PlayerEast.GetResourceOnDemand(Resource.Ore, neededOre, true);
                    neededOre = neededOre - producedOreThis - producedOreWest - producedOreEast;

                    // reset usedOnDemandResource map 
                    this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
                    score = ScoreAfterPlayingCard(game, null);
                    Dictionary<string, int> thisUsedResourceBckp = this.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    Dictionary<string, int> westUsedResourceBckp = this.PlayerWest.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    Dictionary<string, int> eastUsedResourceBckp = this.PlayerEast.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // check if enough resources to build wonder before resource card
                    int neededWood = this.Board.HighestCosts[Resource.Wood],
                        neededStone = this.Board.HighestCosts[Resource.Stone],
                        neededClay = this.Board.HighestCosts[Resource.Clay],
                        neededOre = this.Board.HighestCosts[Resource.Ore];

                    int producedWoodThis = this.GetResourceOnDemand(Resource.Wood, neededWood, false);
                    int producedWoodWest = this.PlayerWest.GetResourceOnDemand(Resource.Wood, neededWood, true);
                    int producedWoodEast = this.PlayerEast.GetResourceOnDemand(Resource.Wood, neededWood, true);
                    neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                    // reset usedOnDemandResource map for next resource calculation
                    this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    int producedStoneThis = this.GetResourceOnDemand(Resource.Stone, neededStone, false);
                    int producedStoneWest = this.PlayerWest.GetResourceOnDemand(Resource.Stone, neededStone, true);
                    int producedStoneEast = this.PlayerEast.GetResourceOnDemand(Resource.Stone, neededStone, true);
                    neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                    // reset usedOnDemandResource map for next resource calculation
                    this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    int producedClayThis = this.GetResourceOnDemand(Resource.Clay, neededClay, false);
                    int producedClayWest = this.PlayerWest.GetResourceOnDemand(Resource.Clay, neededClay, true);
                    int producedClayEast = this.PlayerEast.GetResourceOnDemand(Resource.Clay, neededClay, true);
                    neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                    // reset usedOnDemandResource map for next resource calculation
                    this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    int producedOreThis = this.GetResourceOnDemand(Resource.Ore, neededOre, false);
                    int producedOreWest = this.PlayerWest.GetResourceOnDemand(Resource.Ore, neededOre, true);
                    int producedOreEast = this.PlayerEast.GetResourceOnDemand(Resource.Ore, neededOre, true);
                    neededOre = neededOre - producedOreThis - producedOreWest - producedOreEast;

                    // reset usedOnDemandResource map 
                    this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
                    score = ScoreAfterPlayingCard(game, null);
                    Dictionary<string, int> usedResourceBckp = this.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
                    if (this.Board.HighestCosts[Resource.Loom] != 0 && this.Board.HighestCosts[Resource.Loom] > loomAmount && this.ChooseExtraManuf(Resource.Loom))
                        loomAmount++;
                    else if (this.Board.HighestCosts[Resource.Glass] != 0 && this.Board.HighestCosts[Resource.Glass] > glassAmount && this.ChooseExtraManuf(Resource.Glass))
                        glassAmount++;
                    else if (this.Board.HighestCosts[Resource.Papyrus] != 0 && this.Board.HighestCosts[Resource.Papyrus] > papyrusAmount && this.ChooseExtraManuf(Resource.Papyrus))
                        papyrusAmount++;

                    // reset usedOnDemandResource map
                    this.UsedOnDemandResource = usedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
                    score = ScoreAfterPlayingCard(game, card);
                    if (this.AmountOfType(CardType.Scientific) < 1 &&
                        this.AmountOfType(CardType.Materials) > 1)
                        score += 10;
                    else
                        score += 2.3;
                }
                else
                    score = ScoreAfterPlayingCard(game, card);

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
                    Card? betterBestCard = nextNeighbor.Age2BuildHeuristic(game, nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            return bestCard;
        }

        public Card? Age1BuildHeuristic(Game game, List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;  // must be less than possible negative score from military loss

            foreach (Card card in cardsList)
            {
                if (card.Id == CardId.EastTradingPost)
                {
                    score = ScoreAfterPlayingCard(game, null);
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
                    score = ScoreAfterPlayingCard(game, null);
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
                    score = ScoreAfterPlayingCard(game, null);
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
                    score = ScoreAfterPlayingCard(game, null);
                    double tradingWeight = 0;
                    Dictionary<string, int> thisUsedResourceBckp = this.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    Dictionary<string, int> westUsedResourceBckp = this.PlayerWest.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    Dictionary<string, int> eastUsedResourceBckp = this.PlayerEast.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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

                        producedWoodThis = this.GetResourceOnDemand(Resource.Wood, neededWood, false);
                        producedWoodWest = this.PlayerWest.GetResourceOnDemand(Resource.Wood, neededWood, true);
                        producedWoodEast = this.PlayerEast.GetResourceOnDemand(Resource.Wood, neededWood, true);
                        neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                        // reset usedOnDemandResource map for next resource calculation
                        this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        producedStoneThis = this.GetResourceOnDemand(Resource.Stone, neededStone, false);
                        producedStoneWest = this.PlayerWest.GetResourceOnDemand(Resource.Stone, neededStone, true);
                        producedStoneEast = this.PlayerEast.GetResourceOnDemand(Resource.Stone, neededStone, true);
                        neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                        // reset usedOnDemandResource map for next resource calculation
                        this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        producedClayThis = this.GetResourceOnDemand(Resource.Clay, neededClay, false);
                        producedClayWest = this.PlayerWest.GetResourceOnDemand(Resource.Clay, neededClay, true);
                        producedClayEast = this.PlayerEast.GetResourceOnDemand(Resource.Clay, neededClay, true);
                        neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                        // reset usedOnDemandResource map for next resource calculation
                        this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        producedOreThis = this.GetResourceOnDemand(Resource.Ore, neededOre, false);
                        producedOreWest = this.PlayerWest.GetResourceOnDemand(Resource.Ore, neededOre, true);
                        producedOreEast = this.PlayerEast.GetResourceOnDemand(Resource.Ore, neededOre, true);
                        neededOre = neededOre - producedOreThis - producedOreWest - producedOreEast;

                        // reset usedOnDemandResource map 
                        this.UsedOnDemandResource = thisUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        this.PlayerWest.UsedOnDemandResource = westUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        this.PlayerEast.UsedOnDemandResource = eastUsedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
                    score = ScoreAfterPlayingCard(game, null);
                    Dictionary<string, int> usedResourceBckp = this.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
                    if (this.Board.HighestCosts[Resource.Loom] != 0 && this.Board.HighestCosts[Resource.Loom] > loomAmount && this.ChooseExtraManuf(Resource.Loom))
                        loomAmount++;
                    else if (this.Board.HighestCosts[Resource.Glass] != 0 && this.Board.HighestCosts[Resource.Glass] > glassAmount && this.ChooseExtraManuf(Resource.Glass))
                        glassAmount++;
                    else if (this.Board.HighestCosts[Resource.Papyrus] != 0 && this.Board.HighestCosts[Resource.Papyrus] > papyrusAmount && this.ChooseExtraManuf(Resource.Papyrus))
                        papyrusAmount++;

                    // reset usedOnDemandResource map
                    this.UsedOnDemandResource = usedResourceBckp.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
                    score = ScoreAfterPlayingCard(game, card);
                    if (this.AmountOfType(CardType.Scientific) < 1 &&
                        this.AmountOfType(CardType.Materials) > 1)
                        score += 4;
                    else
                        score += 2.3;
                }
                else if (card.Type == CardType.Civilian)
                {
                    score = ScoreAfterPlayingCard(game, card);
                    score -= 0.7;
                }
                else
                    score = ScoreAfterPlayingCard(game, card);

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
                    Card? betterBestCard = nextNeighbor.Age1BuildHeuristic(game, nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            if (bestCard == null && !isNeighbor)
            { }

            return bestCard;
        }

        public int ScoreAfterPlayingCard(Game g, Card? card)
        {
            // backup relevant player data before scoring
            //          Player related:  VictoryTokens, DefeatTokens, ResourcesDict, PlayedCards, VictoryPoints,
            //          Card related:    ExtraScience,
            //
            int victoryTokensBckp = this.VictoryTokens;
            int defeatTokensBckp = this.DefeatTokens;
            int victoryPointsBckp = this.VictoryPoints;
            Dictionary<Resource, int> resourcesBckp = this.Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            List<Card> playedCardsBckp = this.PlayedCards.Select(e => e).ToList();
            bool extraScienceBckp = this.ExtraScience;

            // play card
            if (card != null)
            {
                this.ApplyCardEffect_GetCost(card);
                this.PlayedCards.Add(card);
            }

            // do battle and copy guild
            this.Battle(g.Era);
            this.CopyGuild();
            // get score
            int score = this.CalculateScore();

            // revert player changes
            this.VictoryTokens = victoryTokensBckp;
            this.DefeatTokens = defeatTokensBckp;
            this.VictoryPoints = victoryPointsBckp;
            this.Resources = resourcesBckp.ToDictionary(entry => entry.Key, entry => entry.Value);
            this.PlayedCards = playedCardsBckp.Select(e => e).ToList();
            this.ExtraScience = extraScienceBckp;

            return score;
        }
    }
}
