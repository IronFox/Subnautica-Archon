using Assets.Behavior.Adapters;
using Assets.Behavior.Util;
using Assets.Behavior.Util.Enabled;
using Assets.Behavior.Util.Undoable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Behavior.Components.Watchdog
{
    public class RigidbodyWatchdog : MonoBehaviour
    {
        public ArchonControl archon;
        private UndoableActions disabled = new UndoableActions();
        protected bool Set(Rigidbody item, bool enable)
        {
            if (!enable)
                new ZeroVelocityAction(item).Do();
            return new NonKinematic(item).SetEnabled(enable)
                | new CollisionsEnabled(item).SetEnabled(enable);
        }

        private int nextInFrames = 0;
        private readonly List<Rigidbody> rigidbodies = new List<Rigidbody>();

        private bool? lastUndone;

        void Update()
        {
            using (var log = Log.NewLazy())
            {
                if (lastUndone != archon.IsBoardedButNotControlled)
                {
                    lastUndone = archon.IsBoardedButNotControlled;

                    if (archon.IsBoardedButNotControlled)
                        disabled.UndoAndClear();
                }
                if (!archon.IsBoardedButNotControlled)
                {
                    if (--nextInFrames < 0)
                    {
                        nextInFrames = 60;
                        rigidbodies.Clear();
                        GetComponentsInChildren(true, rigidbodies);

                        disabled.UndoAndClearBatches(c =>
                             !(c is Rigidbody b)
                             || !b.transform.IsChildOf(transform)
                        );
                        var include = rigidbodies
                            .Where(r =>
                                r.gameObject != gameObject
                            && !r.transform.IsChildOf(archon.bayControl.dockedSubRoot)
                            );
                        include.Disable(disabled, true);
                    }
                }
            }
        }
    }
}
