using System;
using System.Collections.Generic;

public interface IBatch
{
    bool Do(IAction action, bool forced = false);
}

public interface IAction : IEquatable<IAction>
{
    bool Do();
    void Undo();
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

public class Undoable
{

    private class CallbackAction : IAction
    {
        private readonly Func<bool> @do;
        private readonly Action undo;

        public CallbackAction(UnityEngine.Object target, Func<bool> @do, Action undo)
        {
            Target = target;
            this.@do = @do;
            this.undo = undo;
        }

        public UnityEngine.Object Target { get; }

        public bool Do()
        {
            return @do?.Invoke() ?? false;
        }

        public bool Equals(IAction other) => false;

        public void Undo()
        {
            undo?.Invoke();
        }
    }
    private class Batch : IBatch, IAction
    {
        private List<IAction> Steps { get; } = new List<IAction>();
        public UnityEngine.Object Target { get; }

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
    private List<IAction> Actions { get; } = new List<IAction>();
    public bool Do(IAction action, bool forced = false)
    {
        bool success = action.Do();
        if (!success && !forced)
            return false;
        var key = new ObjectReference(action.Target);
        if (!Map.TryGetValue(key, out var slot))
        {
            slot = Actions.Count;
            Actions.Add(action);
            Map[key] = slot;
        }
        else
            Actions[slot] = action;
        return success;
    }

    public IBatch AddOrReplaceBatch(UnityEngine.Object owner)
    {
        var b = new Batch(owner);
        var key = new ObjectReference(owner);
        if (!Map.TryGetValue(key, out var slot))
        {
            slot = Actions.Count;
            Actions.Add(b);
            Map[key] = slot;
        }
        else
            Actions[slot] = b;
        return b;
    }
    
    public IBatch GetOrAddBatch(UnityEngine.Object owner)
    {
        var key = new ObjectReference(owner);
        if (!Map.TryGetValue(key, out var slot) || !(Actions[slot] is Batch))
        {
            slot = Actions.Count;
            var b = new Batch(owner);
            Actions.Add(b);
            Map[key] = slot;
            return b;
        }
        else
            return (Batch)Actions[slot];
    }

    public void UndoAndClear()
    {
        UndoAll();
        Clear();
    }

    public void UndoAll()
    {
        foreach (var a in Actions)
            a.Undo();
    }

    public void Clear()
    {
        Actions.Clear();
        Map.Clear();
    }

    public bool RedoAll()
    {
        bool rs = false;
        foreach (var a in Actions)
            rs |= a.Do();
        return rs;
    }


}

