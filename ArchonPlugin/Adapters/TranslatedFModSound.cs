using AVS.Interfaces;
using UnityEngine;

namespace Subnautica_Archon.Adapters
{
    internal record TranslatedFModSound(ISoundSource AvsSound, SoundConfig Config) : IInstantiatedSound
    {
        public bool Died => AvsSound.Died;

        public bool ApplyLiveChanges(SoundConfig cfg)
        {
            if (cfg.AudioClip != Config.AudioClip)
                return false;
            if (cfg.Is3D != Config.Is3D)
                return false;
            if (cfg.Loop != Config.Loop)
                return false;
            if (!Mathf.Approximately(cfg.MaxDistance, Config.MaxDistance))
                return false;
            if (!Mathf.Approximately(cfg.MinDistance, Config.MinDistance))
                return false;
            
            AvsSound.ApplyLiveChanges(new(Volume: cfg.Volume, Pitch: cfg.Pitch));
            return true;
        }

        public void Dispose()
        {
            AvsSound.Dispose();
        }
    }
}