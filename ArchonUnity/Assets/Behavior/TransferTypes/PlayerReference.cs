using Assets.Behavior.Util;
using Assets.Behavior.Util.Undoable;
using UnityEngine;

namespace Assets.Behavior.TransferTypes
{
    /// <summary>
    /// Reference to a player in the scene, consisting of a root GameObject and a camera Transform.
    /// The camera may be a child of the root, or it may be detached (e.g. in seated mode).
    /// </summary>
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

        internal void DisableCollidersAndRigidbodies(UndoableActions undo)
        {
            if (!Root)
                return;
            Root.DisableAllEnabledColliders(undo);
            Root.DisableRigidbodies(undo);
        }

    }
}