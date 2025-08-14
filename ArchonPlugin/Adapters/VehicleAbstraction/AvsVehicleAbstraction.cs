using AVS.BaseVehicle;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class AvsVehicleAbstraction : IVehicleAbstraction
    {
        public AvsVehicleAbstraction(AvsVehicle vehicle)
        {
            Vehicle = vehicle;
        }

        public AvsVehicle Vehicle { get; }

        public bool IsVanilla => false;

        public PingInstance PingInstance => Vehicle.HudPingInstance;

        public void BeginHelmControl()
        {
            Vehicle.ClosestPlayerEntry();
            Vehicle.BeginMainHelmControl();
        }

        public void DockVehicle()
        {
            Vehicle.DockVehicle();
        }

        public void UndockVehicle(bool boardPlayer)
        {
            Vehicle.UndockVehicle(boardPlayer: boardPlayer);
        }
    }
}