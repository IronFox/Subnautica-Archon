namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class AvsReflectionVehicleAbstraction : IVehicleAbstraction
    {
        public AvsReflectionVehicleAbstraction(AvsReflectionVehicle avsVehicle)
        {
            AvsVehicle = avsVehicle;
        }

        public AvsReflectionVehicle AvsVehicle { get; }

        public bool IsVanilla => false;

        public PingInstance PingInstance => AvsVehicle.PingInstance;

        public void BeginHelmControl()
        {
            AvsVehicle.PlayerEntry();
            AvsVehicle.BeginPiloting();
        }

        public void DockVehicle()
        {
            AvsVehicle.OnVehicleDocked(default);
        }

        public void UndockVehicle(bool boardPlayer)
        {
            AvsVehicle.OnVehicleUndocked(boardPlayer);
        }
    }
}