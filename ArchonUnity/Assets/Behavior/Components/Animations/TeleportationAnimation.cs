using Assets.Behavior.Adapters;
using Assets.Behavior.TransferTypes;
using UnityEngine;

namespace Assets.Behavior.Components.Animations
{
    /// <summary>
    /// Controls the teleportation animation for a teleportation pad.
    /// </summary>
    [RequireComponent(typeof(SoundAdapter))]
    public class TeleportationAnimation : MonoBehaviour
    {
        public float secondsToTeleport = 100;
        private float actualProgress;
        public float opacity;
        public float progressChangeSpeed = 0.1f;
        public SoundAdapter chargeUpSound;
        public SoundAdapter teardownSound;
        private float chargeUpLen;
        private Renderer[] renderers;
        private bool playingChargeUp = false;
        private bool playingTeardown = false;
        internal TeleportationType type;
        private bool wasActive = false;

        // Start is called before the first frame update
        void Awake()
        {
            chargeUpLen = chargeUpSound.clip.length;
            renderers = GetComponentsInChildren<Renderer>();
        }

        // Update is called once per frame
        void Update()
        {
            if (type == TeleportationType.None && secondsToTeleport > chargeUpLen && actualProgress <= 0)
            {
                if (wasActive)
                {
                    wasActive = false;
                    renderers.ForEach(r => r.enabled = false);
                }
            }
            else
            {
                if (!wasActive)
                {
                    wasActive = true;
                    renderers.ForEach(r => r.enabled = true);
                }
                float progress = secondsToTeleport < chargeUpLen ? (1f - secondsToTeleport / chargeUpLen) : 0;
                if (progress < actualProgress)
                {
                    actualProgress -= progressChangeSpeed * Time.deltaTime;
                    if (progress > actualProgress)
                        actualProgress = progress;
                }
                else if (progress > actualProgress)
                {
                    actualProgress += progressChangeSpeed * Time.deltaTime;
                    if (progress < actualProgress)
                        actualProgress = progress;
                }
                if (progress > 0 && !playingChargeUp)
                {
                    playingChargeUp = true;
                    chargeUpSound.Play();
                }
                opacity = (1f - Mathf.Pow(1f - actualProgress, 3f)) * 0.3f;

                if (progress == 0)
                {
                    playingChargeUp = false;
                    playingTeardown = false;
                }
                int at = 0;
                renderers.ForEach(r =>
                {
                    r.material.SetFloat("_Opacity", opacity);
                    r.material.SetVector("_Center", transform.position);
                    r.material.SetFloat("_Seed", 1f + (at++) * 0.1f);
                    r.transform.rotation = Quaternion.identity; //force upright

                });
                wasActive = true;
            }
        }

        internal void SignalTeleported()
        {
            if (!playingTeardown)
            {
                playingTeardown = true;
                teardownSound.Play();
            }
        }
    }

}