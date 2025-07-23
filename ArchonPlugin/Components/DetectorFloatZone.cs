using AVS.Interfaces;

namespace Subnautica_Archon.Components
{
    internal class DetectorFloatZone : IFloatZone
    {
        public PlayerDetector PlayerDetector { get; }

        public DetectorFloatZone(PlayerDetector playerDetector)
        {
            PlayerDetector = playerDetector;
        }
        public bool IsPlayerInZone(Player player)
            => PlayerDetector.HasPlayer;
    }
}
