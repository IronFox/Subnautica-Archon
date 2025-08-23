using UnityEngine;
using Subnautica_Archon.Util;

namespace Subnautica_Archon.Components
{
    internal class DockableMemory : MonoBehaviour
    {
        public IDockable? Dockable { get; set; }

        internal void Start()
        {
            if (Dockable.IsNull())
            {
                Destroy(this);
            }
        }
    }
}
