using AVS.Log;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class VFVehicleAbstraction : IVehicleAbstraction
    {
        public VFVehicleAbstraction(VFVehicle vfVehicle)
        {
            VfVehicle = vfVehicle;
        }

        public VFVehicle VfVehicle { get; }

        public bool IsVanilla => false;

        public PingInstance PingInstance => VfVehicle.HudPingInstance;

        public void BeginHelmControl()
        {
            VfVehicle.PlayerEntry();
            VfVehicle.BeginPiloting();
        }

        public void DockVehicle()
        {
            VfVehicle.OnVehicleDocked(default);
        }

        public void UndockVehicle(bool boardPlayer)
        {
            using var log = SmartLog.LazyFor(VfVehicle.RMC, parameters: Params.Of(boardPlayer));
            if (boardPlayer)
                VfVehicle.OnVehicleUndocked();
            else
            {
                var wasScuttled = VfVehicle.isScuttled;
                VfVehicle.isScuttled = true;   //prevent automatic player boarding
                VfVehicle.OnVehicleUndocked();
                VfVehicle.isScuttled = wasScuttled;
            }
        }
    }
}