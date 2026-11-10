using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class HalikarnassosA : Wonder
    {
        public HalikarnassosA() : base(WonderId.HalikarnassosA, "Halikarnassos A", Resource.Loom,
            Cost((Resource.Clay, 2)),
            Cost((Resource.Ore, 3)),
            Cost((Resource.Loom, 2)))
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
                    player.DiscardFree = true;
                    break;
                case 3:
                    WonderPoints += 7;
                    break;
            }
        }
    }
}
