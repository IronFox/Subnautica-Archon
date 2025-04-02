using System;
using UnityEngine;

public readonly struct PlayerReference
{
    public GameObject Root { get; }
    public Transform CameraRoot { get; }
    public bool IsSet => Root;

    public bool HasDetachedHead => !CameraRoot.IsChildOf(Root.transform);

    public PlayerReference(GameObject root, Transform cameraRoot)
    {
        Root = root;
        CameraRoot = cameraRoot;
    }

    public static implicit operator bool(PlayerReference player) => player.IsSet;

    internal void DisableCollidersAndRigidbodies(Undoable undo)
    {
        if (!Root)
            return;
        Root.DisableAllEnabledColliders(undo);
        Root.DisableRigidbodies(undo);
    }
}