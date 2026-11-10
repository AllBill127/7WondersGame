using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class HalikarnassosB : Wonder
    {
        public HalikarnassosB() : base(WonderId.HalikarnassosB, "Halikarnassos B", Resource.Loom,
            Cost((Resource.Ore, 2)),
            Cost((Resource.Clay, 3)),
            Cost((Resource.Glass, 1), (Resource.Loom, 1), (Resource.Papyrus, 1)))
        {
        }

        protected override void ApplyStageEffect(PlayerState player, int stage)
        {
            switch (stage)
            {
                case 1:
                    WonderPoints += 2;
                    player.DiscardFree = true;
                    break;
                case 2:
                    WonderPoints += 1;
                    player.DiscardFree = true;
                    break;
                case 3:
                    player.DiscardFree = true;
                    break;
            }
        }
    }
}
