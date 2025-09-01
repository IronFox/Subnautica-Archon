using Assets.Behavior.Util;
using UnityEngine;

namespace Assets.Behavior.Components.Watchdog
{
    /// <summary>
    /// Component that watches a set of colliders, ensuring they stay enabled or disabled as requested.
    /// This is useful to prevent other components or scripts from changing collider states unexpectedly.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Behavior/Components/Other/ColliderWatchdog")]
    public class ColliderWatchdog : BaseWatchdog<Collider>
    {

        protected override bool ChangeEnabled(Collider c, bool enable)
        {
            bool rs = false;
            if (enable)
            {
                if (!c.enabled)
                {
                    c.enabled = true;
                    rs = true;
                }
                if (!c.gameObject.activeInHierarchy)
                {
                    c.gameObject.RequireActive(transform);
                    rs = true;
                }
            }
            else
            {
                if (c.enabled)
                {
                    c.enabled = false;
                    rs = true;
                }
            }
            return rs;
        }
    }

}