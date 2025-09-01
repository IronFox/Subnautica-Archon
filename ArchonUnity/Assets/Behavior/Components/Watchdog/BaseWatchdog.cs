using Assets.Behavior.Adapters;
using Assets.Behavior.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Behavior.Components.Watchdog
{
    public abstract class BaseWatchdog<T> : MonoBehaviour where T : UnityEngine.Component
    {
        private IEnumerator<T> enumerator;
        private bool currentlyInDisabled = true;


        private void MonitorNext()
        {
            using (var log = Log.NewLazy(GetType().Name))
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


        protected abstract bool ChangeEnabled(T item, bool enable);

        /// <summary>
        /// Includes the given set of entries, enabling or disabling them as specified.
        /// </summary>
        /// <param name="entries">Entries to start watching or change state of</param>
        /// <param name="enable">True if the given set should be enabled, false if disabled</param>
        internal void Include(IEnumerable<T> entries, bool enable)
        {
            using (var log = Log.NewLazy(GetType().Name))
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

        private ComponentSet<T> Enabled { get; } = new ComponentSet<T>();
        private ComponentSet<T> Disabled { get; } = new ComponentSet<T>();
    }
}
