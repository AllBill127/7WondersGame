using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class EphesosB : Wonder
    {
        public EphesosB() : base(WonderId.EphesosB, "Ephesos B", Resource.Papyrus,
            Cost((Resource.Stone, 2)),
            Cost((Resource.Wood, 2)),
            Cost((Resource.Glass, 1), (Resource.Loom, 1), (Resource.Papyrus, 1)))
        {
        }

        protected override void ApplyStageEffect(Player player, int stage)
        {
            switch (stage)
            {
                case 1:
                    WonderPoints += 2;
                    player.Resources[Resource.Coins] += 4;
                    break;
                case 2:
                    WonderPoints += 3;
                    player.Resources[Resource.Coins] += 4;
                    break;
                case 3:
                    WonderPoints += 5;
                    player.Resources[Resource.Coins] += 4;
                    break;
            }
        }
    }
}
