using Assets.Behavior.Util;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Behavior.Adapters
{
    /// <summary>
    /// Adapter that plays a sound on the given GameObject, using the given AudioClip and parameters.
    /// The sound is automatically re-instantiated if the clip or parameters change significantly.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Behavior/Adapters/SoundAdapter")]

    public class SoundAdapter : MonoBehaviour
    {
        public IInstantiatedSound Sound { get; private set; }
        public AudioClip clip;

        public float volume = 1;
        public bool play = true;
        public float minDistance = 1f;
        public float maxDistance = 500f;
        public bool is3D = true;
        public float pitch = 1f;
        public bool loop = false;

        private int DeadForFrames { get; set; } = 0;


        void Start()
        {

        }

        void OnDestroy()
        {
            Sound?.Dispose();
            Sound = null;
        }

        // Update is called once per frame
        void Update()
        {
            if (clip != null && play)
            {
                var cfg = GetCurrentConfig();

                if (Sound == null || (DeadForFrames > 10 && loop) || !Sound.ApplyLiveChanges(cfg))
                {
                    using (var log = Log.New())
                        log.Write($"Reinstantiating sound {this.NiceName()} for clip {clip.name}");
                    Sound?.Dispose();
                    Sound = SoundCreator.Instantiate(cfg);
                    DeadForFrames = 0;
                }
            }
            else if (Sound != null)
            {
                Sound.Dispose();
                Sound = null;
            }

            if (Sound != null && Sound.Died)
            {
                DeadForFrames++;
            }
        }

        public void Play()
        {
            if (clip != null)
            {
                var cfg = GetCurrentConfig();
                Sound?.Dispose();
                play = true;
                Sound = SoundCreator.Instantiate(cfg);
            }
        }

        private SoundConfig GetCurrentConfig()
        {
            return new SoundConfig(
                gameObject,
                clip,
                volume: volume,
                minDistance: minDistance,
                maxDistance: maxDistance,
                is3D: is3D,
                pitch: pitch,
                loop: loop
                );
        }


        public static ISoundCreator SoundCreator { get; set; } = new DefaultSoundCreator();
        //new DefaultSoundCreator();

    }

    public interface IInstantiatedSound : IDisposable
    {
        SoundConfig Config { get; }

        bool Died { get; }

        /// <summary>
        /// Attempts to apply the given configuration to the local sound
        /// </summary>
        /// <param name="cfg">Updated configuration</param>
        /// <returns>True if the change could be applied, false if the change is too significant, and the sound needs to be recreated</returns>
        bool ApplyLiveChanges(SoundConfig cfg);

    }

    public readonly struct SoundConfig
    {
        public GameObject Owner { get; }
        public AudioClip AudioClip { get; }
        public float Pitch { get; }
        public bool Is3D { get; }
        public float Volume { get; }
        public bool Loop { get; }
        public float MinDistance { get; }
        public float MaxDistance { get; }

        public SoundConfig(GameObject owner,
            AudioClip audioClip,
            float volume = 1f,
            float minDistance = 1f,
            float maxDistance = 500f,
            bool loop = false,
            bool is3D = true,
            float pitch = 1f)
        {
            Owner = owner;
            AudioClip = audioClip;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            Pitch = pitch;
            Is3D = is3D;
            Volume = volume;
            Loop = loop;
        }


    }



    public interface ISoundCreator
    {
        IInstantiatedSound Instantiate(SoundConfig soundConfig);
    }

    public class DefaultSoundCreator : ISoundCreator
    {
        public IInstantiatedSound Instantiate(SoundConfig soundConfig)
        {
            var source = soundConfig.Owner.AddComponent<AudioSource>();
            source.clip = soundConfig.AudioClip;
            source.playOnAwake = true;
            source.pitch = soundConfig.Pitch;
            source.minDistance = soundConfig.MinDistance;
            source.maxDistance = soundConfig.MaxDistance;
            source.spatialBlend = soundConfig.Is3D ? 1 : 0;
            source.volume = soundConfig.Volume;
            source.loop = soundConfig.Loop;
            AudioPatcher.Patch(source);
            source.Play();

            //using (var log = Log.New())
            //  log.Write($"Playing sound {soundConfig.AudioClip.name} on {soundConfig.Owner.name} (is3D={soundConfig.Is3D}, loop={soundConfig.Loop}, volume={soundConfig.Volume})");

            return new DefaultSound(source, soundConfig);
        }
    }

    public class EmulatedSpacialSoundCreator : ISoundCreator
    {
        public IInstantiatedSound Instantiate(SoundConfig soundConfig)
        {
            var source = soundConfig.Owner.AddComponent<AudioSource>();
            source.clip = soundConfig.AudioClip;
            source.playOnAwake = true;
            source.pitch = soundConfig.Pitch;
            source.minDistance = soundConfig.MinDistance;
            source.maxDistance = soundConfig.MaxDistance;
            source.spatialBlend = 0;
            source.volume = soundConfig.Volume;
            source.loop = soundConfig.Loop;

            var emulator = soundConfig.Owner.AddComponent<SpatialSoundEmulator>();
            emulator.pitch = soundConfig.Pitch;
            emulator.volume = soundConfig.Volume;
            AudioPatcher.Patch(source);
            source.Play();

            return new EmulatedSpacialSound(emulator, source, soundConfig);
        }
    }

    internal class EmulatedSpacialSound : IInstantiatedSound
    {
        public EmulatedSpacialSound(SpatialSoundEmulator emulator, AudioSource source, SoundConfig config)
        {
            Emulator = emulator;
            Source = source;
            Config = config;
        }

        public SpatialSoundEmulator Emulator { get; }
        public AudioSource Source { get; }

        public SoundConfig Config { get; private set; }

        public bool Died => !Emulator;

        public bool ApplyLiveChanges(SoundConfig cfg)
        {
            if (cfg.AudioClip != Source.clip)
                return false;
            var blend = cfg.Is3D ? 1 : 0;
            Source.spatialBlend = blend;
            Source.pitch = cfg.Pitch;
            Source.volume = cfg.Volume;
            Source.maxDistance = cfg.MaxDistance;
            Source.minDistance = cfg.MinDistance;
            Emulator.pitch = cfg.Pitch;
            Emulator.volume = cfg.Volume;

            if (cfg.Volume < 0.01f)
            {
                if (Source.isPlaying)
                    Source.Stop();
            }
            else if (!Source.isPlaying)
                Source.Play();

            Config = cfg;
            return true;
        }

        public void Dispose()
        {
            GameObject.Destroy(Source);
            GameObject.Destroy(Emulator);
        }
    }

    internal class DefaultSound : IInstantiatedSound
    {
        public AudioSource Source { get; }
        public SoundConfig Config { get; private set; }

        public bool Died => !Source;

        private float hasPlayedFor = 0;

        public DefaultSound(AudioSource audioSource, SoundConfig config)
        {
            Source = audioSource;
            Config = config;
        }

        public void Dispose()
        {
            Object.Destroy(Source);
        }

        public bool ApplyLiveChanges(SoundConfig cfg)
        {
            if (cfg.AudioClip != Source.clip)
                return false;
            Source.pitch = cfg.Pitch;
            Source.volume = cfg.Volume;
            Source.maxDistance = cfg.MaxDistance;
            Source.minDistance = cfg.MinDistance;
            Source.spatialBlend = cfg.Is3D ? 1 : 0;
            if (Source.isPlaying)
                hasPlayedFor += Time.deltaTime;
            if (cfg.Volume < 0.01f)
            {
                if (Source.isPlaying)
                    Source.Stop();
            }
            else if (!Source.isPlaying && (cfg.Loop || hasPlayedFor < cfg.AudioClip.length * 0.5f))
                Source.Play();

            Config = cfg;
            return true;
        }
    }
}