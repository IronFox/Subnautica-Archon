using UnityEngine;

namespace Subnautica_Archon.Components
{
    internal class DockableMemory : MonoBehaviour
    {
        public IDockable? Dockable { get; set; }

        internal void Start()
        {
            if (Dockable == null)
            {
                Destroy(this);
            }
        }
    }
}
