namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class NullVehicleAbstraction : IVehicleAbstraction
    {
        public bool IsVanilla => false;

        public PingInstance PingInstance => new PingInstance();

        public void BeginHelmControl()
        { }

        public void DockVehicle()
        { }

        public void UndockVehicle(bool boardPlayer)
        { }
    }
}