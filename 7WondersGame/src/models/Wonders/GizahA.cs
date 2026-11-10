using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class GizahA : Wonder
    {
        public GizahA() : base(WonderId.GizahA, "Gizah A", Resource.Stone,
            Cost((Resource.Stone, 2)),
            Cost((Resource.Wood, 3)),
            Cost((Resource.Stone, 4)))
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
                    WonderPoints += 5;
                    break;
                case 3:
                    WonderPoints += 7;
                    break;
            }
        }
    }
}
