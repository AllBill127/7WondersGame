using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class BabylonA : Wonder
    {
        public BabylonA() : base(WonderId.BabylonA, "Babylon A", Resource.Clay,
            Cost((Resource.Clay, 2)),
            Cost((Resource.Wood, 3)),
            Cost((Resource.Clay, 4)))
        {
        }

        protected override void ApplyStageEffect(PlayerState player, int stage)
        {
            switch (stage)
            {
                case 1:
                    WonderPoints += 3;
                    break;
                case 3:
                    WonderPoints += 7;
                    break;
            }
        }
    }
}
