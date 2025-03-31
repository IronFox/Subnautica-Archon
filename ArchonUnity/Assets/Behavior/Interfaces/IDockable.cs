using System.Collections.Generic;
using UnityEngine;

public interface IDockable
{
    /// <summary>
    /// Dockable is now a child, docking animation is starting
    /// </summary>
    void BeginDocking();
    /// <summary>
    /// Dockable is in final location, docking animation done, closing bay
    /// </summary>
    void EndDocking();
    /// <summary>
    /// Dockable is in final location, everything is disabled, bay is closed
    /// </summary>
    void OnDockingDone();
    void BeginUndocking();
    void EndUndocking();
    /// <summary>
    /// Called once per Update() while waiting for the bay door to close
    /// </summary>
    void UpdateWaitingForBayDoorClose();

    IEnumerable<T> GetAllComponents<T>() where T: Component;

    GameObject GameObject { get; }

}