using UnityEngine;

public interface IDockable
{
    void OnBeginDockAnimation();
    void OnEndDockAnimation();
    void OnBeginUndockAnimation();
    void OnEndUndockAnimation();
    GameObject GameObject { get; }

}