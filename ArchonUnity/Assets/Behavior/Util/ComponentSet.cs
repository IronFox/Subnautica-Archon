using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Stores components of given type.
/// Call Clean() once per
/// </summary>
/// <typeparam name="T"></typeparam>
public class ComponentSet<T> : IDisposable, IEnumerable<T> where T:Component
{
    private class ValidEnumerator : IEnumerator<T>
    {
        public ValidEnumerator(ComponentSet<T> owner)
        {
            Owner = owner;
            Nested = owner.Content.Values.GetEnumerator();
        }

        public ComponentSet<T> Owner { get; }
        public IEnumerator<T> Nested { get; }

        public T Current => Nested.Current;

        object IEnumerator.Current => Nested.Current;

        public void Dispose()
        {
            Nested.Dispose();
        }

        public bool MoveNext()
        {
            while (Nested.MoveNext())
            {
                if (Nested.Current && Owner.AdditionalValidityCheck(Nested.Current))
                    return true;
            }
            return false;
        }

        public void Reset()
        {
            Nested.Reset();
        }
    }

    public int VersionNumber {get; private set; }
    public ComponentSet(UnityEngine.Object owner, Func<T, bool> additionalValidityCheck)
    {
        AdditionalValidityCheck = additionalValidityCheck ?? (_ => true);
        Owner = owner;
    }

    private Dictionary<int, T> Content { get; } = new Dictionary<int, T>();

    private Func<T, bool> AdditionalValidityCheck { get; }
    private UnityEngine.Object Owner { get; }

    private IEnumerator<KeyValuePair<int,T>> CleanEnum { get; set; }
    public DateTime LastChange { get; private set; }
    public DateTime LastCheck { get; private set; }
    private List<int> RemoveOnNextReset {get; } = new List<int>();

    public void Update()
    {
        try
        {
            if (CleanEnum == null)
            {
                bool anyChanged = false;
                foreach (var item in RemoveOnNextReset)
                    if (Content.Remove(item))
                        anyChanged = true;
                RemoveOnNextReset.Clear();
                if (anyChanged)
                {
                    LastChange = DateTime.Now;
                    VersionNumber++;
                }
                CleanEnum = Content.GetEnumerator();
            }
            LastCheck = DateTime.Now;
            if (!CleanEnum.MoveNext())
            {
                CleanEnum = null;
            }
            else
            {
                var c = CleanEnum.Current;
                if (!c.Value || !AdditionalValidityCheck(c.Value))
                {
                    RemoveOnNextReset.Add(c.Key);
                }
            }
        }
        catch (Exception ex)
        {
            LogConfig.Default.LogException($"ComponentSet.Update()",ex);
        }
    }

    //public void Clean()
    //{
    //    List<int> remove = null;
    //    foreach (var c in content)
    //        if (!c.Value || !AdditionalValidityCheck(c.Value))
    //            (remove ?? (remove = new List<int>())).Add(c.Key);
    //    if (remove != null)
    //        foreach (var c in remove)
    //            content.Remove(c);
    //}

    public void Add(T item)
    {
        if (AdditionalValidityCheck(item))
        {
            Content[item.GetInstanceID()] = item;
            CleanEnum = null;
            LastChange = DateTime.Now;
            VersionNumber++;
        }
    }

    public void Remove(T item)
    {
        if (Content.Remove(item.GetInstanceID()))
        {
            CleanEnum = null;
            LastChange = DateTime.Now;
            VersionNumber++;
        }
    }

    public IEnumerator<T> GetEnumerator()
        => new ValidEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator()
        => new ValidEnumerator(this);

    public void Dispose()
    {
        LogConfig.Default.LogWarning($"ComponentSet dispose");
    }

    public void UpdateIfChanged(ref int versionNumber, ref T[] array)
    {
        if (versionNumber != VersionNumber)
        {
            versionNumber = VersionNumber;
            array = this.ToArray();
        }
    }
}