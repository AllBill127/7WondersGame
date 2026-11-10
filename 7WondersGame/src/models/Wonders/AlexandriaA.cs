using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class AlexandriaA : Wonder
    {
        public AlexandriaA() : base(WonderId.AlexandriaA, "Alexandria A", Resource.Glass,
            Cost((Resource.Stone, 2)),
            Cost((Resource.Ore, 2)),
            Cost((Resource.Glass, 2)))
        {
        }

        protected override void ApplyStageEffect(Player player, int stage)
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
