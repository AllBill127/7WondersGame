using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class RhodosB : Wonder
    {
        public RhodosB() : base(WonderId.RhodosB, "Rhodos B", Resource.Ore,
            Cost((Resource.Stone, 3)),
            Cost((Resource.Ore, 4)))
        {
        }

        protected override void ApplyStageEffect(PlayerState player, int stage)
        {
            switch (stage)
            {
                case 1:
                    WonderPoints += 3;
                    player.Resources[Resource.Shields] += 1;
                    player.Resources[Resource.Coins] += 3;
                    break;
                case 2:
                    WonderPoints += 4;
                    player.Resources[Resource.Shields] += 1;
                    player.Resources[Resource.Coins] += 4;
                    break;
            }
        }
    }
}
