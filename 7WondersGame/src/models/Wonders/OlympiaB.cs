using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class OlympiaB : Wonder
    {
        public OlympiaB() : base(WonderId.OlympiaB, "Olympia B", Resource.Wood,
            Cost((Resource.Wood, 2)),
            Cost((Resource.Stone, 2)),
            Cost((Resource.Ore, 2), (Resource.Loom, 1)))
        {
        }

        protected override void ApplyStageEffect(Player player, int stage)
        {
            switch (stage)
            {
                case 1:
                    player.WonderRawCheap = true;
                    break;
                case 2:
                    WonderPoints += 5;
                    break;
            }
        }
    }
}
