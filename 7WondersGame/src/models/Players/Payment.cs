namespace _7WondersGame.src.models.Players
{
    /// <summary>
    /// How a player pays for a card or wonder stage: the coins given to each neighbour for the resources bought
    /// from them. A card's own coin cost (for example Tree Farm's 1 coin) is paid separately.
    /// </summary>
    public readonly record struct PaymentPlan(int ToWest, int ToEast)
    {
        public int Total => ToWest + ToEast;

        /// <summary>Everything comes from the player's own city.</summary>
        public static PaymentPlan Free => default;
    }

    /// <summary>
    /// Finds the cheapest way to pay a cost (E2; fixes R7 and R8). It only reads the players and changes nothing.
    /// <list type="bullet">
    /// <item>Free, from the buyer's own city: fixed production (brown and grey cards, the board's resource), the
    /// either/or brown cards, Caravansery, Forum and Alexandria's wildcards. Each either/or source gives one unit.</item>
    /// <item>Bought from a neighbour: their fixed production and either/or brown cards (not their yellow cards or
    /// wonder stages), 2 coins a unit, or 1 with the matching trading post, the Marketplace or Olympia B.</item>
    /// </list>
    /// Every way of using the either/or sources is tried, and a purchase may be split between the neighbours.
    /// The plan with the lowest total wins. Among equally cheap plans the one paying least to the east wins
    /// (west first), so agents never choose between them.
    /// </summary>
    public static class PaymentPlanner
    {
        // Bit i stands for the Resource with value i: Wood, Ore, Clay, Stone, Loom, Glass, Papyrus.
        private const int Goods = 7;
        private const int Raw = 0b0001111;
        private const int Manufactured = 0b1110000;

        private static int Bit(Resource r) => 1 << (int)r;

        /// <summary>
        /// The resources an either/or card offers as a bit mask, or 0 for any other card. Neighbours can only buy
        /// from the brown ones (<paramref name="forNeighbour"/>), not from Caravansery or Forum.
        /// </summary>
        public static int EitherOrMask(CardId card, bool forNeighbour) => card switch
        {
            CardId.TreeFarm => Bit(Resource.Wood) | Bit(Resource.Clay),
            CardId.ForestCave => Bit(Resource.Wood) | Bit(Resource.Ore),
            CardId.TimberYard => Bit(Resource.Wood) | Bit(Resource.Stone),
            CardId.Excavation => Bit(Resource.Clay) | Bit(Resource.Stone),
            CardId.ClayPit => Bit(Resource.Clay) | Bit(Resource.Ore),
            CardId.Mine => Bit(Resource.Ore) | Bit(Resource.Stone),
            CardId.Caravansery => forNeighbour ? 0 : Raw,
            CardId.Forum => forNeighbour ? 0 : Manufactured,
            _ => 0,
        };

        /// <summary>
        /// The cheapest plan for <paramref name="buyer"/> to pay <paramref name="cost"/>, or null if they can't
        /// (the resources aren't available, or buying them plus the coin cost is more than their coins).
        /// </summary>
        public static PaymentPlan? Plan(PlayerState buyer, IReadOnlyDictionary<Resource, int> cost)
        {
            int budget = buyer.Resources[Resource.Coins] - (cost.TryGetValue(Resource.Coins, out int coinCost) ? coinCost : 0);
            if (budget < 0)
                return null;

            // What the fixed production doesn't cover. Usually nothing, and then no search is needed.
            int[]? need = null;
            foreach (var (resource, amount) in cost)
            {
                if (resource > Resource.Papyrus)
                    continue;
                int missing = amount - buyer.Resources[resource];
                if (missing > 0)
                {
                    need ??= new int[Goods];
                    need[(int)resource] = missing;
                }
            }
            if (need == null)
                return PaymentPlan.Free;

            PaymentPlan? best = new Search(buyer, need).Run();
            return best is { } plan && plan.Total <= budget ? plan : null;
        }

        private sealed class Search
        {
            private readonly int[] need;
            private readonly List<int> ownSources = new();
            private readonly List<int> sellerSources = new();   // west neighbour's first, then east's
            private readonly int westSourceCount;
            private readonly int[] westSupply = new int[Goods], eastSupply = new int[Goods];
            private readonly int[] westPrice = new int[Goods], eastPrice = new int[Goods];
            private int bestTotal = int.MaxValue, bestEast = int.MaxValue;

            public Search(PlayerState buyer, int[] need)
            {
                this.need = need;

                foreach (Card card in buyer.PlayedCards)
                {
                    int mask = EitherOrMask(card.Id, forNeighbour: false);
                    if (mask != 0)
                        ownSources.Add(mask);
                }
                if (buyer.HasExtraRaw())
                    ownSources.Add(Raw);
                if (buyer.HasExtraManuf())
                    ownSources.Add(Manufactured);

                AddSeller(buyer.PlayerWest, westSupply);
                westSourceCount = sellerSources.Count;
                AddSeller(buyer.PlayerEast, eastSupply);

                for (int g = 0; g < Goods; g++)
                {
                    bool raw = (Raw & (1 << g)) != 0;
                    westPrice[g] = raw ? (buyer.RawCheapWest || buyer.WonderRawCheap ? 1 : 2) : (buyer.ManufCheap ? 1 : 2);
                    eastPrice[g] = raw ? (buyer.RawCheapEast || buyer.WonderRawCheap ? 1 : 2) : (buyer.ManufCheap ? 1 : 2);
                }
            }

            private void AddSeller(PlayerState seller, int[] supply)
            {
                for (int g = 0; g < Goods; g++)
                    supply[g] = seller.Resources[(Resource)g];
                foreach (Card card in seller.PlayedCards)
                {
                    int mask = EitherOrMask(card.Id, forNeighbour: true);
                    if (mask != 0)
                        sellerSources.Add(mask);
                }
            }

            public PaymentPlan? Run()
            {
                UseOwn(0);
                return bestTotal == int.MaxValue ? null : new PaymentPlan(bestTotal - bestEast, bestEast);
            }

            private int NeedMask()
            {
                int mask = 0;
                for (int g = 0; g < Goods; g++)
                    if (need[g] > 0)
                        mask |= 1 << g;
                return mask;
            }

            /// <summary>
            /// Gives each of the buyer's own either/or sources to one still-needed resource, trying every choice.
            /// A free unit never makes a plan dearer, so a source is only left unused when it offers nothing needed.
            /// </summary>
            private void UseOwn(int i)
            {
                if (bestTotal == 0)
                    return;
                int needed = NeedMask();
                if (needed == 0)
                {
                    Consider(0, 0);
                    return;
                }
                if (i == ownSources.Count)
                {
                    UseSellers(0);
                    return;
                }
                int options = ownSources[i] & needed;
                if (options == 0)
                {
                    UseOwn(i + 1);
                    return;
                }
                for (int g = 0; g < Goods; g++)
                {
                    if ((options & (1 << g)) == 0)
                        continue;
                    need[g]--;
                    UseOwn(i + 1);
                    need[g]++;
                }
            }

            /// <summary>
            /// Gives each neighbour either/or source to one resource the buyer still needs and that neighbour
            /// doesn't already offer enough of, trying every choice; then prices the purchase.
            /// </summary>
            private void UseSellers(int i)
            {
                if (i == sellerSources.Count)
                {
                    Price();
                    return;
                }
                int[] supply = i < westSourceCount ? westSupply : eastSupply;
                bool any = false;
                for (int g = 0; g < Goods; g++)
                {
                    if ((sellerSources[i] & (1 << g)) == 0 || supply[g] >= need[g])
                        continue;
                    any = true;
                    supply[g]++;
                    UseSellers(i + 1);
                    supply[g]--;
                }
                if (!any)
                    UseSellers(i + 1);
            }

            /// <summary>Buys each missing resource from the cheaper neighbour first (west when equal).</summary>
            private void Price()
            {
                int toWest = 0, toEast = 0;
                for (int g = 0; g < Goods; g++)
                {
                    int n = need[g];
                    if (n == 0)
                        continue;
                    if (westPrice[g] <= eastPrice[g])
                    {
                        int fromWest = Math.Min(n, westSupply[g]);
                        if (n - fromWest > eastSupply[g])
                            return;
                        toWest += fromWest * westPrice[g];
                        toEast += (n - fromWest) * eastPrice[g];
                    }
                    else
                    {
                        int fromEast = Math.Min(n, eastSupply[g]);
                        if (n - fromEast > westSupply[g])
                            return;
                        toEast += fromEast * eastPrice[g];
                        toWest += (n - fromEast) * westPrice[g];
                    }
                }
                Consider(toWest + toEast, toEast);
            }

            private void Consider(int total, int toEast)
            {
                if (total < bestTotal || total == bestTotal && toEast < bestEast)
                {
                    bestTotal = total;
                    bestEast = toEast;
                }
            }
        }
    }
}
