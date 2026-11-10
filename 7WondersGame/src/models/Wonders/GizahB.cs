using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class GizahB : Wonder
    {
        public GizahB() : base(WonderId.GizahB, "Gizah B", Resource.Stone,
            Cost((Resource.Wood, 2)),
            Cost((Resource.Stone, 3)),
            Cost((Resource.Clay, 3)),
            Cost((Resource.Stone, 4), (Resource.Papyrus, 1)))
        {
        }

        protected override void ApplyStageEffect(Player player, int stage)
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
                    WonderPoints += 5;
                    break;
                case 4:
                    WonderPoints += 7;
                    break;
            }
        }
    }
}
