using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class OlympiaA : Wonder
    {
        public OlympiaA() : base(WonderId.OlympiaA, "Olympia A", Resource.Wood,
            Cost((Resource.Wood, 2)),
            Cost((Resource.Stone, 2)),
            Cost((Resource.Ore, 2)))
        {
        }

        protected override void ApplyStageEffect(PlayerState player, int stage)
        {
            switch (stage)
            {
                case 1:
                    WonderPoints += 3;
                    break;
                case 2:
                    player.FreeCardOnce = true;
                    break;
                case 3:
                    WonderPoints += 7;
                    break;
            }
        }
    }
}
