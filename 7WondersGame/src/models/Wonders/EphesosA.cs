using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class EphesosA : Wonder
    {
        public EphesosA() : base(WonderId.EphesosA, "Ephesos A", Resource.Papyrus,
            Cost((Resource.Stone, 2)),
            Cost((Resource.Wood, 2)),
            Cost((Resource.Papyrus, 2)))
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
                    player.Resources[Resource.Coins] += 9;
                    break;
                case 3:
                    WonderPoints += 7;
                    break;
            }
        }
    }
}
