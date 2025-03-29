using System;
using System.Collections;


public class MultiFrameJob
{
    public MultiFrameJob(params Func<object, AdvanceJob>[] actions)
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


public readonly struct AdvanceJob
{
    public IEnumerable Next { get; }

    public AdvanceJob(IEnumerable next)
    {
        Next = next;
    }
}