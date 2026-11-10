using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class AlexandriaB : Wonder
    {
        public AlexandriaB() : base(WonderId.AlexandriaB, "Alexandria B", Resource.Glass,
            Cost((Resource.Clay, 2)),
            Cost((Resource.Wood, 2)),
            Cost((Resource.Stone, 3)))
        {
        }

        protected override void ApplyStageEffect(PlayerState player, int stage)
        {
            switch (stage)
            {
                case 3:
                    WonderPoints += 7;
                    break;
            }
        }
    }
}
