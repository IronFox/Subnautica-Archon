using Assets.Behavior.Adapters;
using UnityEngine;

namespace Assets.Behavior.Components.Other
{
    /// <summary>
    /// Controls a hatch that opens when the player is nearby.
    /// </summary>
    [RequireComponent(typeof(Animation))]
    [AddComponentMenu("Behavior/Components/Other/HatchControl")]
    public class HatchControl : MonoBehaviour
    {
        public PlayerDetector proximity;
        public PlayerDetector closeProximity;
        private Animation hatchAnimation;
        public float progress = 0;
        public float openSeconds = 1.0f;
        public bool wasPlaying = false;
        public SoundAdapter whirlSound;
        public SoundAdapter slideSound;

        private float initialWhirlSoundVolume;
        private float initialSlideSoundVolume;

        void Awake()
        {
            if (whirlSound)
                initialWhirlSoundVolume = whirlSound.volume;
            if (slideSound)
                initialSlideSoundVolume = slideSound.volume;
        }

        // Start is called before the first frame update
        void Start()
        {
            hatchAnimation = GetComponent<Animation>();
        }

        private float Interval(float value, float min, float max)
        {
            if (value < min || value > max)
                return 0;
            if (min == max) return 1;
            float smooth = (max - min) / 4;
            return M.Smoothstep(min, min + smooth, value)
                * (1f - M.Smoothstep(max - smooth, max, value));
        }

        // Update is called once per frame
        void Update()
        {
            bool play = false;// (--forcePlayFor) >= 0;

            if (closeProximity.HasPlayer)
            {
                if (progress < 1f)
                {
                    progress += Time.deltaTime / openSeconds * 2;
                    progress = Mathf.Clamp01(progress);
                    play = true;
                }
                //if (progress < 1.0f)
                //{
                //    progress = 1;
                //    play = true;
                //}
            }
            else if (proximity.HasPlayer)
            {
                if (progress < 1f)
                {
                    progress += Time.deltaTime / openSeconds;
                    progress = Mathf.Clamp01(progress);
                    play = true;
                }
            }
            else
            {
                if (progress > 0.0f)
                {
                    progress -= Time.deltaTime / openSeconds;
                    progress = Mathf.Clamp01(progress);
                    play = true;
                }
            }

            if (whirlSound != null)
            {
                float v = Interval(progress, 0, 0.7f);
                whirlSound.play = v > 0;
                whirlSound.volume = v * initialWhirlSoundVolume;
                whirlSound.pitch = 0.5f + 0.3f * progress;
            }
            if (slideSound != null)
            {
                slideSound.volume = Interval(progress, 0.5f, 0.99f) * initialSlideSoundVolume;
                slideSound.play = true;
            }

            if (!play && !wasPlaying)
            {
                hatchAnimation.Stop();
                return;
            }

            if (!hatchAnimation.isPlaying)
                hatchAnimation.Play();
            //if (hatchAnimation.isPlaying != play)
            //    if (play)
            //    {
            //        hatchAnimation.Play();
            //    }
            //    else
            //        hatchAnimation.Stop();
            //if (play)
            foreach (AnimationState state in hatchAnimation)
                state.normalizedTime = progress;
            wasPlaying = play;

        }


    }

}