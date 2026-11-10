using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class BabylonB : Wonder
    {
        public BabylonB() : base(WonderId.BabylonB, "Babylon B", Resource.Clay,
            Cost((Resource.Clay, 1), (Resource.Loom, 1)),
            Cost((Resource.Wood, 2), (Resource.Glass, 1)),
            Cost((Resource.Clay, 3), (Resource.Papyrus, 1)))
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
                    player.PlaySeventh = true;
                    break;
            }
        }
    }
}
