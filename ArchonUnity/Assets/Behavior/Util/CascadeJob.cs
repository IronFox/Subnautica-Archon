using System;
using System.Collections;
using System.Collections.Generic;


public class CascadeJob
{
    public CascadeJob(params Func<object, AdvanceJob>[] actions)
    {
        Actions = actions;
    }

    public Func<object, AdvanceJob>[] Actions { get; }

    private int at = 0;
    private IEnumerator current = null;
    private AdvanceJob lastResult;

    public void Next()
    {
        if (current == null || at >= Actions.Length)
        {
            at = 0;
            if (Actions.Length == 0)
                return;
            lastResult = Actions[0](null);
            current = lastResult.Next?.GetEnumerator();
            at++;
        }
        else
        {
            if (current.MoveNext())
            {
                lastResult = Actions[at](current.Current);
            }
            else
            {
                current = lastResult.Next?.GetEnumerator();
                at++;
            }
        }
    }
}




public class CascadeJob<T0, T1>
{
    public CascadeJob(
        Func<IEnumerable<T0>> a0,
        Func<T0, IEnumerable<T1>> a1,
        Action<T1> a2
        )
    {
        A0 = a0;
        A1 = a1;
        A2 = a2;
    }
    public Func<IEnumerable<T0>> A0 { get; }
    public Func<T0, IEnumerable<T1>> A1 { get; }
    public Action<T1> A2 { get; }



    public IEnumerator RunAsCoroutine()
    {
        while (true)
        {
            var rs = A0();
            yield return null;
            List<T1> nextItems = new List<T1>();
            foreach (var item in rs)
            {
                var r1 = A1(item);
                yield return null;
                nextItems.AddRange(r1);
            }
            foreach (var item in nextItems)
            {
                A2(item);
                yield return null;
            }
        }
    }
}





public readonly struct AdvanceJob
{
    public IEnumerable Next { get; }

    public AdvanceJob(IEnumerable next)
    {
        Next = next;
    }
}

public readonly struct AdvanceJob<T>
{
    public IEnumerable<T> Next { get; }

    public AdvanceJob(IEnumerable<T> next)
    {
        Next = next;
    }
}