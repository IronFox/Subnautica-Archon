using Subnautica_Archon.Adapters;
using UnityEngine;
using Subnautica_Archon.Util;

namespace Subnautica_Archon.Components
{
    internal class DockableMemory : MonoBehaviour
    {
        public DockableVehicle? Dockable { get; set; }

        internal void Start()
        {
            if (Dockable.IsNull())
            {
                Destroy(this);
            }
        }
    }
}
