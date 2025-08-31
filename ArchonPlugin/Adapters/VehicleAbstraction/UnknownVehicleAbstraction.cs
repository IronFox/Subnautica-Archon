using AVS;
using Subnautica_Archon.Util;
using Subnautica_Archon.Util.Reflection;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class UnknownVehicleAbstraction(RootModController rmc, Vehicle vehicle) : IVehicleAbstraction
    {
        public bool IsVanilla => false;

        public RootModController RMC { get; } = rmc;
        public Vehicle Vehicle { get; } = vehicle;

        public PingInstance PingInstance
        {
            get
            {
                var pingInstance = FieldAdapter.OfPublic<PingInstance>(RMC, Vehicle, "pingInstance");
                if (pingInstance.IsValid)
                    return pingInstance.Value;
                return Vehicle.subName.pingInstance;
            }
        }

        public void DockVehicle() { }
        public void BeginHelmControl()
        {
            new MethodAdapter<Player, bool, bool>(RMC, Vehicle, "EnterVehicle").Invoke(Player.main, true, false);
            new MethodAdapter(RMC, Vehicle, "OnPilotModeBegin").Invoke();
        }

        public void UndockVehicle(bool boardPlayer)
        { }
    }
}