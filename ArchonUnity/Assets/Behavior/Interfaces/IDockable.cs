using System;
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
    /// <summary>
    /// Signals that this vehicle was loaded in docked status from a savegame and should be treated such
    /// </summary>
    void RestoreDockedStateFromSaveGame();

    /// <summary>
    /// Signals that the dockable has temporarily been removed from the archon
    /// </summary>
    void OnUndockedForSaving();
    /// <summary>
    /// Signals that the dockable has been re-integrated after being offloaded for saving
    /// </summary>
    void OnRedockedAfterSaving();

    /// <summary>
    /// Queries all child components of given type from the local dockable.
    /// Contained player components, if any, should not be returned.
    /// </summary>
    /// <typeparam name="T">Type to query</typeparam>
    /// <returns>All components of given type suitable for manipulation by the docking bay</returns>
    IEnumerable<T> GetAllComponents<T>() where T: Component;
    IEnumerable<GameObject> GetAllObjects();
    GameObject GameObject { get; }
    /// <summary>
    /// True if behaviours should be unfrozen immediately on undock. Vanilla vehicles need this
    /// </summary>
    bool ShouldUnfreezeImmediately { get; }
    /// <summary>
    /// True if this dockable requires a vertical orientation on release
    /// </summary>
    bool UndockUpright { get; }

    /// <summary>
    /// Gets the axis aligned bounds of the local dockable from its point of origin in local space.
    /// Transform scale, if any, should be applied, position or rotation should not
    /// </summary>
    Bounds LocalBounds { get; }

    /// <summary>
    /// Persistently tags the dockable
    /// </summary>
    /// <param name="tag">Tag to add</param>
    void Tag(string tag);
    /// <summary>
    /// Persistently untags the dockable
    /// </summary>
    /// <param name="tag">Tag to remove</param>
    void Untag(string tag);
    /// <summary>
    /// Checks if a tag is present in this dockable
    /// </summary>
    /// <param name="tag">Tag to check</param>
    /// <returns>True if the given tag is present</returns>
    bool IsTagged(string tag);
}