using AVS;
using Subnautica_Archon.Util;
using Subnautica_Archon.Util.Reflection;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class UnknownVehicleAbstraction : IVehicleAbstraction
    {
        public UnknownVehicleAbstraction(RootModController rmc, Vehicle vehicle)
        {
            Rmc = rmc;
            Vehicle = vehicle;

        }

        public bool IsVanilla => false;

        public RootModController Rmc { get; }
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
            new MethodAdapter<Player, bool, bool>(Rmc, Vehicle, "EnterVehicle").Invoke(Player.main, true, false);
            new MethodAdapter(Rmc, Vehicle, "OnPilotModeBegin").Invoke();
        }

        public void UndockVehicle(bool boardPlayer)
        { }
    }
}