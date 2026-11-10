using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class RhodosA : Wonder
    {
        public RhodosA() : base(WonderId.RhodosA, "Rhodos A", Resource.Ore,
            Cost((Resource.Wood, 2)),
            Cost((Resource.Clay, 3)),
            Cost((Resource.Ore, 4)))
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
                    player.Resources[Resource.Shields] += 2;
                    break;
                case 3:
                    WonderPoints += 7;
                    break;
            }
        }
    }
}
