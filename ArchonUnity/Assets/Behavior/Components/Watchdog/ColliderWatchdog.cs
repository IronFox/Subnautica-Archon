using Assets.Behavior.Adapters;
using Assets.Behavior.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Behavior.Components.Watchdog
{
    /// <summary>
    /// Component that watches a set of colliders, ensuring they stay enabled or disabled as requested.
    /// This is useful to prevent other components or scripts from changing collider states unexpectedly.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Behavior/Components/Other/ColliderWatchdog")]
    public class ColliderWatchdog : MonoBehaviour
    {

        private IEnumerator<Collider> enumerator;
        private bool currentlyInDisabled = true;


        private void MonitorNext()
        {
            using (var log = Log.NewLazy())
            {
                if (enumerator == null)
                {
                    currentlyInDisabled = !currentlyInDisabled;
                    enumerator = currentlyInDisabled
                        ? Disabled.GetEnumerator()
                        : Enabled.GetEnumerator();
                }
                if (!enumerator.MoveNext())
                {
                    enumerator.Dispose();
                    enumerator = null;
                    return;
                }
                var c = enumerator.Current;

                if (currentlyInDisabled)
                {
                    if (ChangeEnabled(c, false))
                    {
                        log.Warn($"{nameof(ColliderWatchdog)}: Disabled {c.ComponentToString(transform)}");
                    }
                }
                else
                {
                    if (ChangeEnabled(c, true))
                        log.Warn($"{nameof(ColliderWatchdog)}: Enabled {c.ComponentToString(transform)}");
                }
            }
        }

        // Update is called once per frame
        public virtual void Update()
        {
            for (int i = 0; i < 10; i++)
            {
                MonitorNext();
            }
        }

        /// <summary>
        /// Includes the given set of entries, enabling or disabling them as specified.
        /// </summary>
        /// <param name="entries">Entries to start watching or change state of</param>
        /// <param name="enable">True if the given set should be enabled, false if disabled</param>
        internal void Include(IEnumerable<Collider> entries, bool enable)
        {
            using (var log = Log.NewLazy())
            {
                var list = entries.ToList();
                foreach (var c in list)
                {
                    if (c)
                    {
                        if (enable)
                        {
                            //if (!EnabledColliders.Contains(c) && !DisabledColliders.Contains(c))
                            //  log.Write($"Registering new as enabled: {c.ComponentToString(transform)}");
                            Enabled.Add(c);
                            Disabled.Remove(c);
                            enumerator = null;
                            ChangeEnabled(c, true);
                            log.Debug($"Enabling {c.ComponentToString(transform)}");
                        }
                        else
                        {
                            //if (!EnabledColliders.Contains(c) && !DisabledColliders.Contains(c))
                            //  log.Write($"Registering new as disabled: {c.ComponentToString(transform)}");
                            Disabled.Add(c);
                            Enabled.Remove(c);
                            enumerator = null;
                            ChangeEnabled(c, false);
                            log.Debug($"Disabling {c.ComponentToString(transform)}");
                        }
                    }
                }

                log.Write($"Changes applied. Enabled now {Enabled.Count()}, disabled now {Disabled.Count()}");
            }
        }

        private ComponentSet<Collider> Enabled { get; } = new ComponentSet<Collider>();
        private ComponentSet<Collider> Disabled { get; } = new ComponentSet<Collider>();























        private bool ChangeEnabled(Collider c, bool enable)
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