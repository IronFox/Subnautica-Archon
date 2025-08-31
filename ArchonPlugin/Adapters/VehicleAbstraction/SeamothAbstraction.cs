using AVS;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class SeamothAbstraction : VanillaVehicleAbstraction<SeaMoth>
    {
        public SeamothAbstraction(RootModController rmc, SeaMoth seaMoth) : base(rmc, seaMoth)
        {
        }

    }
}