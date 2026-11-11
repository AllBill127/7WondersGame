using _7WondersGame.src.models.Effects;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models
{
    public static partial class Rules
    {
        // =========== Before scoring ===========
        /// <summary>
        /// Give Victory and Defeat tokens to the player based on era by battling each neighbor
        /// </summary>
        /// <param name="era"></param>
        public static void Battle(this PlayerState p, int era)
        {
            int eraValue = 1;
            // Age I   ->  +1 victory token
            // Age II  ->  +3 victory tokens
            // Age III ->  +5 victory tokens
            switch (era)
            {
                case 2:
                    eraValue = 3;
                    break;
                case 3:
                    eraValue = 5;
                    break;
                default:
                    break;
            }

            int shields = p.Resources[Resource.Shields];

            // Battle with east neighbor
            if (shields > p.PlayerEast.Resources[Resource.Shields])
            {
                p.VictoryTokens += eraValue;
            }
            else if (shields < p.PlayerEast.Resources[Resource.Shields])
            {
                p.DefeatTokens += 1;
            }

            // Battle with west neighbor
            if (shields > p.PlayerWest.Resources[Resource.Shields])
            {
                p.VictoryTokens += eraValue;
            }
            else if (shields < p.PlayerWest.Resources[Resource.Shields])
            {
                p.DefeatTokens += 1;
            }
        }

        /// <summary>
        /// Olympia B stage 3: at the end of the game, copy one guild built by a neighbour, as if built. <br/>
        /// - each candidate is scored with the real CalculateScore(); the best one is kept (ties go to the first candidate, west then east) <br/>
        /// - the copy stays in PlayedCards, so Shipowners counts it and a copied Scientists Guild gives its wildcard <br/>
        /// - a guild the player already has is skipped (no two identical structures) <br/>
        /// - should be called before calculating scores <br/>
        /// Sources: https://rules.dized.com/7-wonders/rule/wonder-boards/the-statue-of-zeus-in-olympia,
        ///          https://rules.dized.com/7-wonders/rule/symbols/guilds
        /// </summary>
        /// <returns>
        /// true if a guild was copied; otherwise false
        /// </returns>
        public static bool CopyGuild(this PlayerState p)
        {
            if (!p.Has(Ability.CopyGuild))
                return false;

            List<Card> candidates = p.PlayerWest.PlayedCards.Concat(p.PlayerEast.PlayedCards)
                .Where(card => card.Type == CardType.Guild && !p.HasPlayedCard(card))
                .Distinct()
                .ToList();

            if (candidates.Count == 0)
                return false;

            // guilds only score at the end (none has an effect when built), so trying one is adding and removing it
            int victoryPointsBckp = p.VictoryPoints;

            Card? bestGuild = null;
            int bestScore = int.MinValue;
            foreach (Card guild in candidates)
            {
                p.PlayedCards.Add(guild);
                int score = p.CalculateScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    bestGuild = guild;
                }
                p.PlayedCards.RemoveAt(p.PlayedCards.Count - 1);
            }
            p.VictoryPoints = victoryPointsBckp;

            p.PlayedCards.Add(bestGuild!);
            return true;
        }

        // =========== End of game scoring related ===========
        /// <summary>
        /// Calculate Civilian (Blue) card VP score for the player
        /// </summary>
        public static int CalculateCivilianScore(this PlayerState p) => p.CardPoints(CardType.Civilian);

        /// <summary>
        /// Calculate Commercial (Yellow) card VP score for the player
        /// </summary>
        public static int CalculateCommercialScore(this PlayerState p) => p.CardPoints(CardType.Commercial);

        /// <summary>
        /// Calculate Money VP score for the player (3 coins = 1 VP)
        /// </summary>
        /// <returns></returns>
        public static int CalculateMoneyScore(this PlayerState p)
        {
            return (int)decimal.Floor(p.Resources[Resource.Coins] / 3);
        }

        /// <summary>
        /// Calculate Guild (Purple) card VP score for the player (the Scientists Guild's wildcard is in the science score)
        /// </summary>
        public static int CalculateGuildScore(this PlayerState p) => p.CardPoints(CardType.Guild);

        /// <summary>The end-game VP of the player's cards of one type.</summary>
        private static int CardPoints(this PlayerState p, CardType type)
        {
            int vp = 0;
            foreach (Card card in p.PlayedCards)
                if (card.Type == type)
                    for (int i = 0; i < card.Effects.Count; i++)
                        vp += card.Effects[i].EndGameVp(p);
            return vp;
        }

        /// <summary>
        /// Calculate Military VP score for the player
        /// </summary>
        /// <returns></returns>
        public static int CalculateMilitaryScore(this PlayerState p)
        {
            return p.VictoryTokens - p.DefeatTokens;
        }

        /// <summary>
        /// Calculate Scientific VP score for the player including extra science structures
        /// </summary>
        /// <returns></returns>
        public static int CalculateScientificScore(this PlayerState p)
        {
            int gear = p.Resources[Resource.Gear],
                tablet = p.Resources[Resource.Tablet],
                compass = p.Resources[Resource.Compass];

            return BestScienceScore(gear, compass, tablet, p.ScienceWildcards());
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
        /// Calculate Wonder VP score for the player
        /// </summary>
        /// <returns></returns>
        public static int CalculateWonderScore(this PlayerState p) => p.Board.WonderPoints(p);

        /// <summary>
        /// Update total Victory Points for the player
        /// </summary>
        /// <returns>
        /// player Victory Points
        /// </returns>
        public static int CalculateScore(this PlayerState p)
        {
            int civilian = p.CalculateCivilianScore();
            int commercial = p.CalculateCommercialScore();
            int money = p.CalculateMoneyScore();
            int guilds = p.CalculateGuildScore();
            int science = p.CalculateScientificScore();
            int military = p.CalculateMilitaryScore();
            int wonder = p.CalculateWonderScore();

            int total = civilian + commercial + money + 
                guilds + science + military + wonder;

            p.VictoryPoints = total;

            return p.VictoryPoints;
        }
    }
}
