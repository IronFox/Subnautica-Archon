using AVS;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class PrawnSuitAbstraction : VanillaVehicleAbstraction<Exosuit>
    {
        public PrawnSuitAbstraction(RootModController rmc, Exosuit prawnSuit) : base(rmc, prawnSuit)
        { }
    }
}