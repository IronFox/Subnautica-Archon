using Assets.Behavior.Adapters;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Behavior.Util.Undoable
{
    public interface IBatch
    {
        bool Do(IAction action, bool forced = false);
    }

    public interface IAction : IEquatable<IAction>
    {
        bool Do();
        void Undo();
        bool TargetIsGone { get; }
        UnityEngine.Object Target { get; }
    }

    public readonly struct ObjectReference : IEquatable<ObjectReference>
    {
        public int InstanceId { get; }
        public UnityEngine.Object Reference { get; }

        public ObjectReference(int instanceId, UnityEngine.Object reference)
        {
            InstanceId = instanceId;
            Reference = reference;
        }

        public ObjectReference(UnityEngine.Object reference)
            : this(reference.GetInstanceID(), reference)
        { }

        public bool IsAlive => Reference;

        public override bool Equals(object obj)
        {
            return obj is ObjectReference reference &&
                   InstanceId == reference.InstanceId;
        }

        public override int GetHashCode()
        {
            return -676353417 + InstanceId.GetHashCode();
        }

        public bool Equals(ObjectReference other) => InstanceId == other.InstanceId;

        public override string ToString() => $"'{Reference.NiceName()}'[{InstanceId}]";
    }

    public class UndoableActions
    {

        private class Batch : IBatch, IAction
        {
            private List<IAction> Steps { get; } = new List<IAction>();
            public UnityEngine.Object Target { get; }

            public bool TargetIsGone => !Target;

            public Batch(UnityEngine.Object target)
            {
                Target = target;
            }
            public bool Do(IAction action, bool forced = false)
            {
                bool success = action.Do();
                if (success || forced)
                {
                    for (int i = Steps.Count - 1; i >= 0; i--)
                        if (Steps[i].Equals(action))
                        {
                            Steps.RemoveAt(i);
                        }
                    Steps.Add(action);
                    return success;
                }
                return false;
            }

            public bool Do()
            {
                bool rs = false;
                foreach (var step in Steps)
                    rs |= step.Do();
                return rs;
            }

            public void Undo()
            {
                foreach (var step in Steps)
                    step.Undo();
            }

            public bool Equals(IAction other) => false;
        }

        private Dictionary<ObjectReference, int> Map { get; } = new Dictionary<ObjectReference, int>();
        private List<IAction> Sequence { get; } = new List<IAction>();
        public bool Do(IAction action, bool forced = false)
        {
            bool success = true;
            try
            {
                success = action.Do();
                if (!success && !forced)
                    return false;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            var key = new ObjectReference(action.Target);
            if (!Map.TryGetValue(key, out var slot))
            {
                slot = Sequence.Count;
                Sequence.Add(action);
                Map[key] = slot;
            }
            else
                Sequence[slot] = action;
            return success;
        }

        public IBatch AddOrReplaceBatch(UnityEngine.Object owner)
        {
            var b = new Batch(owner);
            var key = new ObjectReference(owner);
            if (!Map.TryGetValue(key, out var slot))
            {
                slot = Sequence.Count;
                Sequence.Add(b);
                Map[key] = slot;
            }
            else
                Sequence[slot] = b;
            return b;
        }

        public IBatch GetOrAddBatch(UnityEngine.Object owner)
        {
            var key = new ObjectReference(owner);
            if (!Map.TryGetValue(key, out var slot) || !(Sequence[slot] is Batch))
            {
                slot = Sequence.Count;
                var b = new Batch(owner);
                Sequence.Add(b);
                Map[key] = slot;
                return b;
            }
            else
                return (Batch)Sequence[slot];
        }


        public void UndoAndClear()
        {
            UndoAll();
            Clear();
        }

        /// <summary>
        /// Selectively purges and undoes batches whose target matches the given predicate.
        /// </summary>
        /// <param name="predicate">
        /// Predicate to match targets of batches to be undone and removed.
        /// </param>
        public void UndoAndClearBatches(Func<UnityEngine.Object, bool> predicate)
        {
            using (var log = Log.NewLazy())
            {
                List<int> toRemove = new List<int>();
                for (int i = 0; i < Sequence.Count; i++)
                {
                    var a = Sequence[i];
                    if (a is Batch b && predicate(b.Target))
                        try
                        {
                            a.Undo();
                            toRemove.Add(i);
                            log.Write($"Undid and removed batch for {b.Target.NiceName()} @{i}/{Sequence.Count}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                }
                for (int i = toRemove.Count - 1; i >= 0; i--)
                {
                    var idx = toRemove[i];
                    var a = Sequence[idx];
                    var key = new ObjectReference(a.Target);
                    Sequence.RemoveAt(idx);
                    Map.Remove(key);
                }
                for (int i = 0; i < Sequence.Count; i++)
                {
                    var a = Sequence[i];
                    var key = new ObjectReference(a.Target);
                    Map[key] = i;
                }
            }
        }

        public void UndoAll()
        {
            foreach (var a in Sequence)
                try
                {
                    a.Undo();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
        }

        public void Clear()
        {
            Sequence.Clear();
            Map.Clear();
        }

        public bool RedoAll()
        {
            bool rs = false;
            foreach (var a in Sequence)
                try
                {
                    rs |= a.Do();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            return rs;
        }


    }


}
