using AVS;
using Subnautica_Archon.Util.Reflection;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal abstract class VanillaVehicleAbstraction<T> : IVehicleAbstraction
        where T : Vehicle
    {
        public RootModController Rmc { get; }
        protected T Vehicle { get; }
        public VanillaVehicleAbstraction(RootModController rmc, T vehicle)
        {
            Rmc = rmc;
            Vehicle = vehicle;
        }
        public bool IsVanilla => true;

        public PingInstance PingInstance => Vehicle.subName.pingInstance;

        public void DockVehicle()
        { }

        public void BeginHelmControl()
        {
            new MethodAdapter<Player, bool, bool>(Rmc, Vehicle, "EnterVehicle").Invoke(Player.main, true, false);
            new MethodAdapter(Rmc, Vehicle, "OnPilotModeBegin").Invoke();
        }

        public void UndockVehicle(bool boardPlayer)
        { }

        //public bool IsPlayerControlling()
        //{
        //    return _vehicle.IsPlayerControlling();
        //}
        //public void SetPlayerControlling(bool isControlling)
        //{
        //    _vehicle.SetPlayerControlling(isControlling);
        //}
    }
}