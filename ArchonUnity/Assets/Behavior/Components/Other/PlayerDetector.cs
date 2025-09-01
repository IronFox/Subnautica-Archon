using Assets.Behavior.Util;
using UnityEngine;

namespace Assets.Behavior.Components.Other
{
    /// <summary>
    /// Detects if the player is within a trigger collider.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Behavior/Components/Other/PlayerDetector")]
    public class PlayerDetector : MonoBehaviour
    {
        private int lastPlayerFrame = -10000;
        private int frameCounter;
        private BoxCollider _collider;
        public void Start()
        {
            _collider = GetComponent<BoxCollider>();
        }
        public void OnTriggerEnter(Collider other)
        {
            var go = other.GetGameObject();
            if (go && PlayerAdapter.Player() == go)
                lastPlayerFrame = frameCounter;
        }
        public void OnTriggerExit(Collider other)
        {
            var go = other.GetGameObject();
            if (go && PlayerAdapter.Player() == go)
                lastPlayerFrame = -1;
        }
        public void OnTriggerStay(Collider other)
        {
            var go = other.GetGameObject();
            if (go && PlayerAdapter.Player() == go)
                lastPlayerFrame = frameCounter;
        }

        public bool HasPlayer => frameCounter - lastPlayerFrame < 3;

        public void FixedUpdate()
        {
            frameCounter++;
        }
    }

}