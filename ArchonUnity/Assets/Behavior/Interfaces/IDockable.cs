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

    /// <summary>
    /// Dockable is awaiting bay door opening
    /// </summary>
    void PrepareUndocking();
    /// <summary>
    /// Bay doors are open. Dockable will be moved out of the bay
    /// </summary>
    void BeginUndocking();
    /// <summary>
    /// Dockable has been moved out of the bay and will now be released
    /// </summary>
    void EndUndocking();
    /// <summary>
    /// Dockable has cleared the bay and can redock
    /// </summary>
    void OnUndockingDone();
    /// <summary>
    /// Called once per Update() while waiting for the bay door to close (after docking)
    /// </summary> 
    void UpdateWaitingForBayDoorClose();
    /// <summary>
    /// Called once per Update() while waiting for the bay doors to open (before undocking)
    /// </summary>
    void UpdateWaitingForBayDoorOpen();

    IEnumerable<T> GetAllComponents<T>() where T: Component;

    GameObject GameObject { get; }

}