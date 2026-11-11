using _7WondersGame.src.models.Effects;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// DeterministicAI's card scores for each era (the old heuristic, unchanged; G replaces it). They read the
    /// players and never change them.
    /// </summary>
    public static class Heuristics
    {
        // =========== Deterministic heuristics ===========
        public static Card? Age3BuildHeuristic(this PlayerState p, List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;

            foreach (Card card in cardsList)
            {
                score = p.ScoreAfterPlayingCard(3, card);

                if (score > maxScore)
                {
                    bestCard = card;
                    maxScore = score;
                }
                else if (!isNeighbor && score == maxScore)
                {
                    // if cards score is equal to max score then choose a card that would be most usefull for neighbor in next turn
                    PlayerState nextNeighbor = p.PlayerWest;
                    List<Card> nextNeighborPlayableCards = nextNeighbor.GetPlayableCards(cardsList);
                    Card? betterBestCard = nextNeighbor.Age3BuildHeuristic(nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            return bestCard;
        }

        public static Card? Age2BuildHeuristic(this PlayerState p, List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;

            foreach (Card card in cardsList)
            {
                if (card.Id == CardId.Vineyard &&
                    p.Resources[Resource.Coins] <= 3)
                {
                    score = p.ScoreAfterPlayingCard(2, null);
                    // add score for gaining money with extra weight 
                    int cardAmount = p.AmountOfType(CardType.Materials) + 
                        p.PlayerEast.AmountOfType(CardType.Materials) + 
                        p.PlayerWest.AmountOfType(CardType.Materials);
                    score += 1 * cardAmount * 2;
                }
                else if (card.Id == CardId.Bazar &&
                    p.Resources[Resource.Coins] <= 3)
                {
                    score = p.ScoreAfterPlayingCard(2, null);
                    // add score for gaining money with extra weight 
                    int cardAmount = p.AmountOfType(CardType.Manufactured) +
                        p.PlayerEast.AmountOfType(CardType.Manufactured) +
                        p.PlayerWest.AmountOfType(CardType.Manufactured);
                    score += 2 * cardAmount * 2;
                }
                else if (card.Id == CardId.Forum)
                {
                    score = p.ScoreAfterPlayingCard(2, null);

                    // get amounts of potencially producable manufectured goods resources
                    int loomAmount = p.Resources[Resource.Loom] +
                        p.PlayerEast.Resources[Resource.Loom] +
                        p.PlayerWest.Resources[Resource.Loom];
                    bool extraManuf = p.Has(Ability.AnyManufactured);
                    if (loomAmount == 0 && extraManuf)
                    {
                        loomAmount++;
                        extraManuf = false;
                    }

                    int glassAmount = p.Resources[Resource.Glass] +
                        p.PlayerEast.Resources[Resource.Glass] +
                        p.PlayerWest.Resources[Resource.Glass];
                    if (glassAmount == 0 && extraManuf)
                    {
                        glassAmount++;
                        extraManuf = false;
                    }

                    int papyrusAmount = p.Resources[Resource.Papyrus] +
                        p.PlayerEast.Resources[Resource.Papyrus] +
                        p.PlayerWest.Resources[Resource.Papyrus];
                    if (papyrusAmount == 0 && extraManuf)
                        papyrusAmount++;

                    // check if enough manufactured goods for wonder building
                    if (p.Board.HighestCosts[Resource.Loom] > loomAmount || 
                        p.Board.HighestCosts[Resource.Glass] > glassAmount ||
                        p.Board.HighestCosts[Resource.Papyrus] > papyrusAmount)
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
                    score = p.ScoreAfterPlayingCard(2, null);

                    // check if enough resources to build wonder before resource card
                    int neededWood = p.Board.HighestCosts[Resource.Wood],
                        neededStone = p.Board.HighestCosts[Resource.Stone],
                        neededClay = p.Board.HighestCosts[Resource.Clay],
                        neededOre = p.Board.HighestCosts[Resource.Ore];

                    int producedWoodThis = p.CountProducible(Resource.Wood, neededWood, false);
                    int producedWoodWest = p.PlayerWest.CountProducible(Resource.Wood, neededWood, true);
                    int producedWoodEast = p.PlayerEast.CountProducible(Resource.Wood, neededWood, true);
                    neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                    int producedStoneThis = p.CountProducible(Resource.Stone, neededStone, false);
                    int producedStoneWest = p.PlayerWest.CountProducible(Resource.Stone, neededStone, true);
                    int producedStoneEast =  p.PlayerEast.CountProducible(Resource.Stone, neededStone, true);
                    neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                    int producedClayThis = p.CountProducible(Resource.Clay, neededClay, false);
                    int producedClayWest = p.PlayerWest.CountProducible(Resource.Clay, neededClay, true);
                    int producedClayEast =  p.PlayerEast.CountProducible(Resource.Clay, neededClay, true);
                    neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                    int producedOreThis = p.CountProducible(Resource.Ore, neededOre, false);
                    int producedOreWest = p.PlayerWest.CountProducible(Resource.Ore, neededOre, true);
                    int producedOreEast =  p.PlayerEast.CountProducible(Resource.Ore, neededOre, true);
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
                    score = p.ScoreAfterPlayingCard(2, null);

                    // check if enough resources to build wonder before resource card
                    int neededWood = p.Board.HighestCosts[Resource.Wood],
                        neededStone = p.Board.HighestCosts[Resource.Stone],
                        neededClay = p.Board.HighestCosts[Resource.Clay],
                        neededOre = p.Board.HighestCosts[Resource.Ore];

                    int producedWoodThis = p.CountProducible(Resource.Wood, neededWood, false);
                    int producedWoodWest = p.PlayerWest.CountProducible(Resource.Wood, neededWood, true);
                    int producedWoodEast = p.PlayerEast.CountProducible(Resource.Wood, neededWood, true);
                    neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                    int producedStoneThis = p.CountProducible(Resource.Stone, neededStone, false);
                    int producedStoneWest = p.PlayerWest.CountProducible(Resource.Stone, neededStone, true);
                    int producedStoneEast = p.PlayerEast.CountProducible(Resource.Stone, neededStone, true);
                    neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                    int producedClayThis = p.CountProducible(Resource.Clay, neededClay, false);
                    int producedClayWest = p.PlayerWest.CountProducible(Resource.Clay, neededClay, true);
                    int producedClayEast = p.PlayerEast.CountProducible(Resource.Clay, neededClay, true);
                    neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                    int producedOreThis = p.CountProducible(Resource.Ore, neededOre, false);
                    int producedOreWest = p.PlayerWest.CountProducible(Resource.Ore, neededOre, true);
                    int producedOreEast = p.PlayerEast.CountProducible(Resource.Ore, neededOre, true);
                    neededOre = neededOre - producedOreThis - producedOreWest - producedOreEast;

                    if (neededWood > 0 ||
                        neededStone > 0 ||
                        neededClay > 0 ||
                        neededOre > 0)
                    {
                        if (card.Id == CardId.Sawmill)
                        {
                            neededWood -= 2;
                            if (p.Board.HighestCosts[Resource.Wood] != 0 && neededWood > 0)
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
                            if (p.Board.HighestCosts[Resource.Stone] != 0 && neededStone > 0)
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
                            if (p.Board.HighestCosts[Resource.Clay] != 0 && neededClay > 0)
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
                            if (p.Board.HighestCosts[Resource.Ore] != 0 && neededOre > 0)
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
                    score = p.ScoreAfterPlayingCard(2, null);

                    // get amounts of potencially producable manufectured goods resources
                    int loomAmount = p.Resources[Resource.Loom] +
                        p.PlayerEast.Resources[Resource.Loom] +
                        p.PlayerWest.Resources[Resource.Loom];

                    int glassAmount = p.Resources[Resource.Glass] +
                        p.PlayerEast.Resources[Resource.Glass] +
                        p.PlayerWest.Resources[Resource.Glass];

                    int papyrusAmount = p.Resources[Resource.Papyrus] +
                        p.PlayerEast.Resources[Resource.Papyrus] +
                        p.PlayerWest.Resources[Resource.Papyrus];

                    // choose extra manuf for first non 0 wonder cost and try then
                    if (p.Board.HighestCosts[Resource.Loom] != 0 && p.Board.HighestCosts[Resource.Loom] > loomAmount && p.Has(Ability.AnyManufactured))
                        loomAmount++;
                    else if (p.Board.HighestCosts[Resource.Glass] != 0 && p.Board.HighestCosts[Resource.Glass] > glassAmount && p.Has(Ability.AnyManufactured))
                        glassAmount++;
                    else if (p.Board.HighestCosts[Resource.Papyrus] != 0 && p.Board.HighestCosts[Resource.Papyrus] > papyrusAmount && p.Has(Ability.AnyManufactured))
                        papyrusAmount++;

                    if (card.Name.Equals("Loom"))
                    {
                        // check if enough loom for wonder building
                        if (p.Board.HighestCosts[Resource.Loom] > loomAmount)
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
                        if (p.Board.HighestCosts[Resource.Glass] > glassAmount)
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
                        if (p.Board.HighestCosts[Resource.Papyrus] > papyrusAmount)
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
                    score = p.ScoreAfterPlayingCard(2, card);
                    if (p.AmountOfType(CardType.Scientific) < 1 &&
                        p.AmountOfType(CardType.Materials) > 1)
                        score += 10;
                    else
                        score += 2.3;
                }
                else
                    score = p.ScoreAfterPlayingCard(2, card);

                if (score > maxScore)
                {
                    bestCard = card;
                    maxScore = score;
                }
                else if (!isNeighbor && score == maxScore)
                {
                    // if cards score is equal to max score then choose a card that would be most usefull for neighbor in next turn
                    PlayerState nextNeighbor = p.PlayerEast;
                    List<Card> nextNeighborPlayableCards = nextNeighbor.GetPlayableCards(cardsList);
                    Card? betterBestCard = nextNeighbor.Age2BuildHeuristic(nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            return bestCard;
        }

        public static Card? Age1BuildHeuristic(this PlayerState p, List<Card> cardsList, bool isNeighbor = false)
        {
            Card? bestCard = null;
            double score, maxScore = -100;  // must be less than possible negative score from military loss

            foreach (Card card in cardsList)
            {
                if (card.Id == CardId.EastTradingPost)
                {
                    score = p.ScoreAfterPlayingCard(1, null);
                    int eastRawAmount = p.PlayerEast.AmountOfType(CardType.Materials);
                    int thisRawAmount = p.AmountOfType(CardType.Materials);
                    if (p.Board.Production <= Resource.Stone)
                        thisRawAmount++;
                    if (p.PlayerEast.Board.Production <= Resource.Stone)
                        eastRawAmount++;

                    if (thisRawAmount <= 2 &&
                        eastRawAmount >= 1)
                        score += 2 + eastRawAmount;     // slightly better than any war or science and better than resources 
                }
                else if (card.Id == CardId.WestTradingPost)
                {
                    score = p.ScoreAfterPlayingCard(1, null);
                    int westRawAmount = p.PlayerWest.AmountOfType(CardType.Materials);
                    int thisRawAmount = p.AmountOfType(CardType.Materials);
                    if (p.Board.Production <= Resource.Stone)
                        thisRawAmount++;
                    if (p.PlayerWest.Board.Production <= Resource.Stone)
                        westRawAmount++;

                    if (thisRawAmount <= 2 &&
                        westRawAmount >= 1)
                        score += 2.3 + westRawAmount;     // slightly better than any war or science and better than resources 
                }
                else if (card.Id == CardId.Marketplace)
                {
                    score = p.ScoreAfterPlayingCard(1, null);
                    int eastManufAmount = p.PlayerEast.AmountOfType(CardType.Manufactured);
                    int westManufAmount = p.PlayerWest.AmountOfType(CardType.Manufactured);
                    int thisManufAmount = p.AmountOfType(CardType.Manufactured);
                    if (Resource.Loom <= p.Board.Production &&
                        p.Board.Production <= Resource.Papyrus)
                        thisManufAmount++;
                    if (Resource.Loom <= p.PlayerWest.Board.Production &&
                        p.Board.Production <= Resource.Papyrus)
                        westManufAmount++;
                    if (Resource.Loom <= p.PlayerEast.Board.Production &&
                        p.Board.Production <= Resource.Papyrus)
                        eastManufAmount++;

                    if (thisManufAmount <= 1 &&
                        (westManufAmount >= 1 || eastManufAmount >= 1))
                        score += 2 + westManufAmount + eastManufAmount;     // slightly better than any war or science and better than resources 
                }
                else if (card.Type == CardType.Materials)
                {
                    score = p.ScoreAfterPlayingCard(1, null);
                    double tradingWeight = 0;

                    // check if enough resources to build wonder before resource card
                    int neededWood, neededStone, neededClay, neededOre, 
                        producedWoodThis, producedWoodWest, producedWoodEast, 
                        producedStoneThis, producedStoneWest, producedStoneEast,
                        producedClayThis, producedClayWest, producedClayEast, 
                        producedOreThis, producedOreWest, producedOreEast;
                    {
                        neededWood = p.Board.HighestCosts[Resource.Wood];
                        neededStone = p.Board.HighestCosts[Resource.Stone];
                        neededClay = p.Board.HighestCosts[Resource.Clay];
                        neededOre = p.Board.HighestCosts[Resource.Ore];

                        producedWoodThis = p.CountProducible(Resource.Wood, neededWood, false);
                        producedWoodWest = p.PlayerWest.CountProducible(Resource.Wood, neededWood, true);
                        producedWoodEast = p.PlayerEast.CountProducible(Resource.Wood, neededWood, true);
                        neededWood = neededWood - producedWoodThis - producedWoodWest - producedWoodEast;

                        producedStoneThis = p.CountProducible(Resource.Stone, neededStone, false);
                        producedStoneWest = p.PlayerWest.CountProducible(Resource.Stone, neededStone, true);
                        producedStoneEast = p.PlayerEast.CountProducible(Resource.Stone, neededStone, true);
                        neededStone = neededStone - producedStoneThis - producedStoneWest - producedStoneEast;

                        producedClayThis = p.CountProducible(Resource.Clay, neededClay, false);
                        producedClayWest = p.PlayerWest.CountProducible(Resource.Clay, neededClay, true);
                        producedClayEast = p.PlayerEast.CountProducible(Resource.Clay, neededClay, true);
                        neededClay = neededClay - producedClayThis - producedClayWest - producedClayEast;

                        producedOreThis = p.CountProducible(Resource.Ore, neededOre, false);
                        producedOreWest = p.PlayerWest.CountProducible(Resource.Ore, neededOre, true);
                        producedOreEast = p.PlayerEast.CountProducible(Resource.Ore, neededOre, true);
                        neededOre = neededOre - producedOreThis - producedOreWest - producedOreEast;
                    }

                    int eastRawAmount = p.PlayerEast.AmountOfType(CardType.Materials);
                    if (p.PlayerEast.Board.Production <= Resource.Stone)
                        eastRawAmount++;
                    int westRawAmount = p.PlayerWest.AmountOfType(CardType.Materials);
                    if (p.PlayerWest.Board.Production <= Resource.Stone)
                        westRawAmount++;
                    if ((p.Has(Ability.RawCheapEast) && eastRawAmount >= 2) ||
                        (p.Has(Ability.RawCheapWest) && westRawAmount >= 2) || 
                        (p.Has(Ability.WonderRawCheap) && 
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
                            if (p.Board.HighestCosts[Resource.Wood] != 0 && neededWood > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (p.Board.HighestCosts[Resource.Wood] != 0 && 
                                (producedWoodThis + producedWoodWest + producedWoodEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.1;    // slightly worse than multichoice 
                            }
                        }
                        else if (card.Id == CardId.StonePit)
                        {
                            neededStone -= 1;
                            if (p.Board.HighestCosts[Resource.Stone] != 0 && neededStone > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (p.Board.HighestCosts[Resource.Stone] != 0 && 
                                (producedStoneThis + producedStoneWest + producedStoneEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.1;    // slightly worse than multichoice 
                            }
                        }
                        else if (card.Id == CardId.ClayPool)
                        {
                            neededClay -= 1;
                            if (p.Board.HighestCosts[Resource.Clay] != 0 && neededClay > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (p.Board.HighestCosts[Resource.Clay] != 0 && 
                                (producedClayThis + producedClayWest + producedClayEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.1;    // slightly worse than multichoice 
                            }
                        }
                        else if (card.Id == CardId.OreVein)
                        {
                            neededOre -= 1;
                            if (p.Board.HighestCosts[Resource.Ore] != 0 && neededOre > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (p.Board.HighestCosts[Resource.Ore] != 0 && 
                                (producedOreThis + producedOreWest + producedOreEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.1;    // slightly worse than multichoice 
                            }
                        }
                        /*else if (card.Id == CardId.TreeFarm || card.Id == CardId.TimberYard || card.Id == CardId.ForestCave)
                        {
                            neededWood -= 1;
                            if (p.Board.HighestCosts[Resource.Wood] != 0 && neededWood - 1 > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.3;     // slightly worse than manufactured goods
                            }
                            else if (p.Board.HighestCosts[Resource.Wood] != 0 &&
                                (producedWoodThis + producedWoodWest + producedWoodEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.3;    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.TimberYard || card.Id == CardId.Mine)
                        {
                            neededStone -= 1;
                            if (p.Board.HighestCosts[Resource.Stone] != 0 && neededStone - 1 > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.3;     // slightly worse than manufactured goods
                            }
                            else if (p.Board.HighestCosts[Resource.Stone] != 0 &&
                                (producedStoneThis + producedStoneWest + producedStoneEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.3;    // slightly better than single resource 
                            }
                        }*/
                        else if (card.Id == CardId.Excavation)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (p.Board.HighestCosts[Resource.Stone] != 0)
                                materialWeight++;
                            if (p.Board.HighestCosts[Resource.Clay] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((p.Board.HighestCosts[Resource.Stone] != 0 &&
                                (producedStoneThis + producedStoneWest + producedStoneEast) <= 1) ||
                                (p.Board.HighestCosts[Resource.Clay] != 0 &&
                                (producedClayThis + producedClayWest + producedClayEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.ClayPit)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (p.Board.HighestCosts[Resource.Clay] != 0)
                                materialWeight++;
                            if (p.Board.HighestCosts[Resource.Ore] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((p.Board.HighestCosts[Resource.Clay] != 0 &&
                                (producedClayThis + producedClayWest + producedClayEast) <= 1) ||
                                (p.Board.HighestCosts[Resource.Ore] != 0 &&
                                (producedOreThis + producedOreWest + producedOreEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.TreeFarm)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (p.Board.HighestCosts[Resource.Clay] != 0)
                                materialWeight++;
                            if (p.Board.HighestCosts[Resource.Wood] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((p.Board.HighestCosts[Resource.Clay] != 0 &&
                                (producedClayThis + producedClayWest + producedClayEast) <= 1) ||
                                (p.Board.HighestCosts[Resource.Wood] != 0 &&
                                (producedWoodThis + producedWoodWest + producedWoodEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.TimberYard)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (p.Board.HighestCosts[Resource.Stone] != 0)
                                materialWeight++;
                            if (p.Board.HighestCosts[Resource.Wood] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((p.Board.HighestCosts[Resource.Stone] != 0 &&
                                (producedStoneThis + producedStoneWest + producedStoneEast) <= 1) ||
                                (p.Board.HighestCosts[Resource.Wood] != 0 &&
                                (producedWoodThis + producedWoodWest + producedWoodEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.ForestCave)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (p.Board.HighestCosts[Resource.Ore] != 0)
                                materialWeight++;
                            if (p.Board.HighestCosts[Resource.Wood] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((p.Board.HighestCosts[Resource.Ore] != 0 &&
                                (producedOreThis + producedOreWest + producedOreEast) <= 1) ||
                                (p.Board.HighestCosts[Resource.Wood] != 0 &&
                                (producedWoodThis + producedWoodWest + producedWoodEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        else if (card.Id == CardId.Mine)
                        {
                            // check if both optional materials are useful for wonder
                            double materialWeight = 0;
                            if (p.Board.HighestCosts[Resource.Ore] != 0)
                                materialWeight++;
                            if (p.Board.HighestCosts[Resource.Stone] != 0)
                                materialWeight++;

                            // check if recomended material amount is produced without card
                            if ((p.Board.HighestCosts[Resource.Ore] != 0 &&
                                (producedOreThis + producedOreWest + producedOreEast) <= 1) ||
                                (p.Board.HighestCosts[Resource.Stone] != 0 &&
                                (producedStoneThis + producedStoneWest + producedStoneEast) <= 1))
                            {
                                score += 2 - tradingWeight * 0.75 + (materialWeight * 0.5);    // slightly better than single resource 
                            }
                        }
                        /*else if (card.Id == CardId.ClayPit || card.Id == CardId.ForestCave || card.Id == CardId.Mine)
                        {
                            neededOre -= 1;
                            if (p.Board.HighestCosts[Resource.Ore] != 0 && neededOre - 1 > 0)
                            {
                                score += (2 - tradingWeight * 0.5) * 1.1;     // slightly worse than manufactured goods
                            }
                            else if (p.Board.HighestCosts[Resource.Ore] != 0 &&
                                (producedOreThis + producedOreWest + producedOreEast + 1) < 2)
                            {
                                score += (2 - tradingWeight * 0.75) * 1.3;    // slightly better than single resource 
                            }
                        }*/
                    }
                }
                else if (card.Type == CardType.Manufactured)
                {
                    score = p.ScoreAfterPlayingCard(1, null);

                    // get amounts of potencially producable manufectured goods resources
                    int loomAmount = p.Resources[Resource.Loom] +
                        p.PlayerEast.Resources[Resource.Loom] +
                        p.PlayerWest.Resources[Resource.Loom];

                    int glassAmount = p.Resources[Resource.Glass] +
                        p.PlayerEast.Resources[Resource.Glass] +
                        p.PlayerWest.Resources[Resource.Glass];

                    int papyrusAmount = p.Resources[Resource.Papyrus] +
                        p.PlayerEast.Resources[Resource.Papyrus] +
                        p.PlayerWest.Resources[Resource.Papyrus];

                    // choose extra manuf for first non 0 wonder cost and try then
                    if (p.Board.HighestCosts[Resource.Loom] != 0 && p.Board.HighestCosts[Resource.Loom] > loomAmount && p.Has(Ability.AnyManufactured))
                        loomAmount++;
                    else if (p.Board.HighestCosts[Resource.Glass] != 0 && p.Board.HighestCosts[Resource.Glass] > glassAmount && p.Has(Ability.AnyManufactured))
                        glassAmount++;
                    else if (p.Board.HighestCosts[Resource.Papyrus] != 0 && p.Board.HighestCosts[Resource.Papyrus] > papyrusAmount && p.Has(Ability.AnyManufactured))
                        papyrusAmount++;

                    if (card.Name.Equals("Loom"))
                    {
                        if ((p.Board.HighestCosts[Resource.Loom]) > loomAmount)
                        {
                            score += 2.5;     // slightly worse than double raw resource when its needed for wonder building
                        }
                    }
                    else if (card.Name.Equals("Glassworks"))
                    {
                        if (p.Board.HighestCosts[Resource.Glass] > glassAmount)
                        {
                            score += 2.5;     // slightly worse than double raw resource when its needed for wonder building
                        }
                    }
                    else if (card.Name.Equals("Press"))
                    {
                        if (p.Board.HighestCosts[Resource.Papyrus] > papyrusAmount)
                        {
                            score += 2.5;     // slightly worse than double raw resource when its needed for wonder building
                        }
                    }
                }
                else if (card.Type == CardType.Scientific)
                {
                    score = p.ScoreAfterPlayingCard(1, card);
                    if (p.AmountOfType(CardType.Scientific) < 1 &&
                        p.AmountOfType(CardType.Materials) > 1)
                        score += 4;
                    else
                        score += 2.3;
                }
                else if (card.Type == CardType.Civilian)
                {
                    score = p.ScoreAfterPlayingCard(1, card);
                    score -= 0.7;
                }
                else
                    score = p.ScoreAfterPlayingCard(1, card);

                if (score > maxScore)
                {
                    bestCard = card;
                    maxScore = score;
                }
                else if (!isNeighbor && score == maxScore)
                {
                    // if cards score is equal to max score then choose a card that would be most usefull for neighbor in next turn
                    PlayerState nextNeighbor = p.PlayerWest;
                    List<Card> nextNeighborPlayableCards = nextNeighbor.GetPlayableCards(cardsList);
                    Card? betterBestCard = nextNeighbor.Age1BuildHeuristic(nextNeighborPlayableCards, isNeighbor: true);
                    if (betterBestCard != null)
                        bestCard = betterBestCard;
                }
            }

            return bestCard;
        }

        /// <summary>
        /// The player's score if the card were built now and the era ended (a battle; Olympia B copies a guild).
        /// Worked out on a copy, so the player doesn't change.
        /// </summary>
        public static int ScoreAfterPlayingCard(this PlayerState p, int era, Card? card)
        {
            PlayerState s = p.Clone();
            if (card != null)
            {
                // added before applying the effect, like a real build (the Lighthouse counts itself)
                s.PlayedCards.Add(card);
                s.ApplyCardEffect(card);
            }
            s.Battle(era);
            s.CopyGuild();
            return s.CalculateScore();
        }

        /// <summary>
        /// How much of <paramref name="needed"/> this player could produce of a resource: its fixed production plus
        /// every either/or source that can give it (for a neighbour, only the brown cards they could sell).
        /// Read-only; the heuristics use it to judge what is missing.
        /// </summary>
        private static int CountProducible(this PlayerState p, Resource resource, int needed, bool isNeighbor)
        {
            int count = p.Resources[resource];
            if (count >= needed) return needed;

            foreach (Card card in p.PlayedCards)
                if (((isNeighbor ? card.SellableChoiceMask : card.ChoiceMask) & (1 << (int)resource)) != 0)
                    count++;
            if (!isNeighbor && (int)resource <= (int)Resource.Stone && p.Has(Ability.AnyRaw)) count++;
            if (!isNeighbor && resource >= Resource.Loom && resource <= Resource.Papyrus && p.Has(Ability.AnyManufactured)) count++;

            return Math.Min(needed, count);
        }
    }
}
