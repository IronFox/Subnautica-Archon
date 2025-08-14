using AVS.BaseVehicle;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    /// <summary>
    /// General purpose interface for other vehicles.
    /// </summary>
    public interface IVehicleAbstraction
    {
        bool IsVanilla { get; }

        void DockVehicle();

        /// <summary>
        /// Boards the craft and begins helm control.
        /// </summary>
        void BeginHelmControl();

        /// <summary>
        /// Notifies the vehicle that it has been undocked.
        /// </summary>
        /// <param name="boardPlayer">If true, the player will be boarded into the vehicle.</param>
        void UndockVehicle(bool boardPlayer);

        PingInstance PingInstance { get; }
    }


    public static class VehicleAbstraction
    {
        public static IVehicleAbstraction ToAbstraction(this Vehicle? vehicle)
        {
            switch (vehicle)
            {
                case Exosuit prawnSuit:
                    return new PrawnSuitAbstraction(prawnSuit);
                case SeaMoth seaMoth:
                    return new SeamothAbstraction(seaMoth);
                case null:
                    return new NullVehicleAbstraction();
            }

            if (VFVehicle.Access(vehicle, out var vfVehicle))
            {
                return new VFVehicleAbstraction(vfVehicle);
            }

            if (AvsReflectionVehicle.Access(vehicle, out var avsVehicle))
            {
                return new AvsReflectionVehicleAbstraction(avsVehicle);
            }
            if (vehicle is AvsVehicle av)
                return new AvsVehicleAbstraction(av);
            return new UnknownVehicleAbstraction(vehicle);
        }

    }
}
