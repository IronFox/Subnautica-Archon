using System;
using System.Collections.Generic;

public class Undoable
{
    private List<Action> Undo { get; } = new List<Action>();
    public void Add(Action undoAction)
    {
        Undo.Add(undoAction);
    }

    public void UndoAll()
    {
        foreach (var a in Undo)
            a();
        Undo.Clear();
    }


}

