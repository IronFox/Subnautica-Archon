using Subnautica_Archon.Util;
using Subnautica_Archon.Util.Reflection;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class UnknownVehicleAbstraction : IVehicleAbstraction
    {
        public UnknownVehicleAbstraction(Vehicle vehicle)
        {
            Vehicle = vehicle;
        }

        public bool IsVanilla => false;

        public Vehicle Vehicle { get; }

        public PingInstance PingInstance
        {
            get
            {
                var pingInstance = FieldAdapter.OfPublic<PingInstance>(Vehicle, "pingInstance");
                if (pingInstance.IsValid)
                    return pingInstance.Value;
                return Vehicle.subName.pingInstance;
            }
        }

        public void DockVehicle() { }
        public void BeginHelmControl()
        {
            new MethodAdapter<Player, bool, bool>(Vehicle, "EnterVehicle").Invoke(Player.main, true, false);
            new MethodAdapter(Vehicle, "OnPilotModeBegin").Invoke();
        }

        public void UndockVehicle(bool boardPlayer)
        { }
    }
}