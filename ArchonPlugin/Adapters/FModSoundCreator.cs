using Assets.Behavior.Adapters;
using AVS.Audio;
using AVS.Util;
using FMOD;
using Subnautica_Archon.Util;
using UnityEngine;

namespace Subnautica_Archon.Adapters
{
    internal class FModSoundCreator : ISoundCreator
    {
        public float HalfDistance { get; set; } = 20f;
        public ArchonModController Amc { get; }

        public FModSoundCreator(ArchonModController amc)
        {
            Amc = amc;
        }
        public IInstantiatedSound? Instantiate(SoundConfig cfg)
        {
            var sound = AVS.Audio.FModSoundCreator.Play(
                new(
                    Owner: cfg.Owner,
                    RMC: Amc,
                    AudioClip: cfg.AudioClip,
                    Loop: cfg.Loop,
                    HalfDistance: Mathf.Max(HalfDistance, cfg.MinDistance * 2),
                    MinDistance: cfg.MinDistance,
                    MaxDistance: cfg.MaxDistance,
                    Settings: new(
                        Volume: cfg.Volume,
                        Pitch: cfg.Pitch
                        )));
            if (sound.IsNull())
                return null;
            return new TranslatedFModSound(sound, cfg);
        }

        internal static void Check(string action, RESULT result)
        {
            if (result != RESULT.OK)
                throw new FModException($"{action} failed with {result}", result);
        }
    }
}