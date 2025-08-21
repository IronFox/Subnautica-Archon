using UnityEngine;

public readonly struct PlayerReference
{
    public GameObject Root { get; }
    public Transform CameraRoot { get; }

    public bool IsSet => Root;

    public bool HasDetachedHead => CameraRoot && !CameraRoot.IsChildOf(Root.transform);

    public PlayerReference(GameObject root, Transform cameraRoot, float headToSeatedHeightDifference)
    {
        Root = root;
        CameraRoot = cameraRoot;
        HeadToSeatedHeightDifference = headToSeatedHeightDifference;
    }

    public override string ToString() => $"Player {Root.NiceName()} -> {CameraRoot.NiceName()} h={HeadToSeatedHeightDifference}";

    /// <summary>
    /// Height difference between the head center location and the seated player root position.
    /// Typically this is around 1.6 meters
    /// </summary>
    public float HeadToSeatedHeightDifference { get; }

    public static implicit operator bool(PlayerReference player) => player.IsSet;

    internal void DisableCollidersAndRigidbodies(Undoable undo)
    {
        if (!Root)
            return;
        Root.DisableAllEnabledColliders(undo);
        Root.DisableRigidbodies(undo);
    }

}