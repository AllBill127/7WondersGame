using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

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

            int this_shields = p.Resources[Resource.Shields];

            // Battle with east neighbor
            if (this_shields > p.PlayerEast.Resources[Resource.Shields])
            {
                p.VictoryTokens += current_era_value;
            }
            else if (this_shields < p.PlayerEast.Resources[Resource.Shields])
            {
                p.DefeatTokens += 1;
            }

            // Battle with west neighbor
            if (this_shields > p.PlayerWest.Resources[Resource.Shields])
            {
                p.VictoryTokens += current_era_value;
            }
            else if (this_shields < p.PlayerWest.Resources[Resource.Shields])
            {
                p.DefeatTokens += 1;
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
        public static bool CopyGuild(this PlayerState p)
        {
            if (p.Board.Id != WonderId.OlympiaB || p.Board.Stage < 3)
                return false;

            List<Card> candidates = p.PlayerWest.PlayedCards.Concat(p.PlayerEast.PlayedCards)
                .Where(card => card.Type == CardType.Guild && !p.HasPlayedCard(card))
                .Distinct()
                .ToList();

            if (candidates.Count == 0)
                return false;

            int victoryPointsBckp = p.VictoryPoints;
            bool extraScienceBckp = p.ExtraScience;

            Card? bestGuild = null;
            int bestScore = int.MinValue;
            foreach (Card guild in candidates)
            {
                p.PlayedCards.Add(guild);
                if (guild.Id == CardId.Scientists)
                    p.ExtraScience = true;

                int score = p.CalculateScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    bestGuild = guild;
                }

                p.PlayedCards.RemoveAt(p.PlayedCards.Count - 1);
                p.ExtraScience = extraScienceBckp;
            }
            p.VictoryPoints = victoryPointsBckp;

            p.PlayedCards.Add(bestGuild!);
            if (bestGuild!.Id == CardId.Scientists)
                p.ExtraScience = true;

            return true;
        }

        // =========== End of game scoring related ===========
        /// <summary>
        /// Calculate Civilian (Blue) card VP score for the player
        /// </summary>
        /// <returns></returns>
        public static int CalculateCivilianScore(this PlayerState p)
        {
            // Each Civilian (blue) structure gives a fixed number of VPs.
            int civil_score = 0;

            foreach (Card card in p.PlayedCards)
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
        /// Calculate Commercial (Yellow) card VP score for the player
        /// </summary>
        /// <returns></returns>
        public static int CalculateCommercialScore(this PlayerState p)
        {
            int comm_score = 0;

            foreach (Card card in p.PlayedCards)
            {
                if (card.Type != CardType.Commercial)
                    continue;

                switch (card.Id)
                {
                    case CardId.Haven:
                        // +1 VP per Raw Material CARD in your own city.
                        comm_score += p.AmountOfType(CardType.Materials);
                        break;

                    case CardId.Lighthouse:
                        // +1 VP per Commercial Structure CARD in your own city.
                        comm_score += p.AmountOfType(CardType.Commercial);
                        break;

                    case CardId.ChamberOfCommerce:
                        // +2 VP per Manufactured Good CARD in your own city.
                        comm_score += p.AmountOfType(CardType.Manufactured) * 2;
                        break;

                    case CardId.Arena:
                        // +1 VP Coin per Wonder stage constructed in your own city.
                        comm_score += p.Board.Stage;
                        break;
                }
            }

            return comm_score;
        }

        /// <summary>
        /// Calculate Money VP score for the player (3 coins = 1 VP)
        /// </summary>
        /// <returns></returns>
        public static int CalculateMoneyScore(this PlayerState p)
        {
            return (int)decimal.Floor(p.Resources[Resource.Coins] / 3);
        }

        /// <summary>
        /// Calculate Guild (Purple) card VP score for the player
        /// </summary>
        /// <returns></returns>
        public static int CalculateGuildScore(this PlayerState p)
        {
            // The guilds (purple) provide several means of gaining victory points, most of
            // which are based on the types of structure a player and/or his neighbors have built.
            int guild_score = 0;

            foreach (Card card in p.PlayedCards)
            {
                if (card.Type != CardType.Guild)
                    continue;

                switch (card.Id)
                {
                    case CardId.Workers:
                        guild_score += p.PlayerEast.AmountOfType(CardType.Materials)
                                     + p.PlayerWest.AmountOfType(CardType.Materials);
                        break;

                    case CardId.Craftsmens:
                        guild_score += 2 * (p.PlayerEast.AmountOfType(CardType.Manufactured)
                                     + p.PlayerWest.AmountOfType(CardType.Manufactured));
                        break;

                    case CardId.Traders:
                        guild_score += p.PlayerEast.AmountOfType(CardType.Commercial)
                                     + p.PlayerWest.AmountOfType(CardType.Commercial);
                        break;

                    case CardId.Philosophers:
                        guild_score += p.PlayerEast.AmountOfType(CardType.Scientific)
                                     + p.PlayerWest.AmountOfType(CardType.Scientific);
                        break;

                    case CardId.Spies:
                        guild_score += p.PlayerEast.AmountOfType(CardType.Military)
                                     + p.PlayerWest.AmountOfType(CardType.Military);
                        break;

                    case CardId.Magistrates:
                        guild_score += p.PlayerEast.AmountOfType(CardType.Civilian)
                                     + p.PlayerWest.AmountOfType(CardType.Civilian);
                        break;

                    case CardId.Shipowners:
                        guild_score += p.AmountOfType(CardType.Materials)
                                     + p.AmountOfType(CardType.Manufactured)
                                     + p.AmountOfType(CardType.Guild);
                        break;

                    case CardId.Strategists:
                        guild_score += p.PlayerEast.DefeatTokens
                                     + p.PlayerWest.DefeatTokens;
                        break;

                    case CardId.Scientists:
                        // computed in CalculateScientificScore()
                        break;

                    case CardId.Builders:
                        guild_score += p.PlayerEast.Board.Stage
                                     + p.PlayerWest.Board.Stage
                                     + p.Board.Stage;
                        break;
                }
            }

            return guild_score;
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

            // calculate extra science
            int sci_extra = 0;
            if (p.ExtraScience)
                sci_extra++;

            WonderId id = p.Board.Id;
            int stage = p.Board.Stage;
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
        /// Calculate Wonder VP score for the player
        /// </summary>
        /// <returns></returns>
        public static int CalculateWonderScore(this PlayerState p)
        {
            if (p.Board.Stage > 0)
                return p.Board.WonderPoints;
            return 0;
        }

        /// <summary>
        /// Update total Victory Points for the player
        /// </summary>
        /// <returns>
        /// player Victory Points
        /// </returns>
        public static int CalculateScore(this PlayerState p)
        {
            int civil_score = p.CalculateCivilianScore();
            int commercial_score = p.CalculateCommercialScore();
            int money_score = p.CalculateMoneyScore();
            int guild_score = p.CalculateGuildScore();
            int science_score = p.CalculateScientificScore();
            int military_score = p.CalculateMilitaryScore();
            int wonder_score = p.CalculateWonderScore();

            int total_score = civil_score + commercial_score + money_score + 
                guild_score + science_score + military_score + wonder_score;

            p.VictoryPoints = total_score;

            return p.VictoryPoints;
        }
    }
}
