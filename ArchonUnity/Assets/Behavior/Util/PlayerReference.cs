using System;
using UnityEngine;

public readonly struct PlayerReference
{
    public GameObject Root { get; }
    public Transform CameraRoot { get; }
    public Action<Vector3> CustomLookInDirection { get; }

    public bool IsSet => Root;

    public bool HasDetachedHead => CameraRoot && !CameraRoot.IsChildOf(Root.transform);

    public PlayerReference(GameObject root, Transform cameraRoot, Action<Vector3> customLookAt)
    {
        Root = root;
        CameraRoot = cameraRoot;
        CustomLookInDirection = customLookAt;
    }

    public static implicit operator bool(PlayerReference player) => player.IsSet;

    internal void DisableCollidersAndRigidbodies(Undoable undo)
    {
        if (!Root)
            return;
        Root.DisableAllEnabledColliders(undo);
        Root.DisableRigidbodies(undo);
    }

    internal void LookInDirection(Vector3 lookAt)
    {
        if (CustomLookInDirection != null)
        {
            CustomLookInDirection(lookAt);
        }
        else if (CameraRoot)
        {
            CameraRoot.forward = lookAt.normalized;
            LockedEuler
                .FromLocal(CameraRoot)
                .ConstrainedHead()
                .ApplyTo(CameraRoot);
        }
        //else if (Root)
        //{
        //    Root.transform.LookAt(lookAt);
        //}
    }
}