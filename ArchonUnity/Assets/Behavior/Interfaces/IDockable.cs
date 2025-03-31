using System.Collections.Generic;
using UnityEngine;

public interface IDockable
{
    void BeginDocking();
    void EndDocking();
    void BeginUndocking();
    void EndUndocking();

    IEnumerable<T> GetAllComponents<T>() where T: Component;

    GameObject GameObject { get; }

}