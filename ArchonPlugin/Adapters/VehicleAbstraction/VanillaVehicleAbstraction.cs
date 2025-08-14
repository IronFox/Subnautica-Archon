using Subnautica_Archon.Util.Reflection;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal abstract class VanillaVehicleAbstraction<T> : IVehicleAbstraction
        where T : Vehicle
    {
        protected T Vehicle { get; }
        public VanillaVehicleAbstraction(T vehicle)
        {
            Vehicle = vehicle;
        }
        public bool IsVanilla => true;

        public PingInstance PingInstance => Vehicle.subName.pingInstance;

        public void DockVehicle()
        { }

        public void BeginHelmControl()
        {
            new MethodAdapter<Player, bool, bool>(Vehicle, "EnterVehicle").Invoke(Player.main, true, false);
            new MethodAdapter(Vehicle, "OnPilotModeBegin").Invoke();
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