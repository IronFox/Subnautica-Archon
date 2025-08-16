using AVS.Interfaces;

namespace Subnautica_Archon.Adapters
{
    internal record TranslatedFModSound(ISoundSource AvsSound, SoundConfig Config) : IInstantiatedSound
    {
        public bool Died => AvsSound.Died;

        public void ApplyLiveChanges(SoundConfig cfg)
        {
            AvsSound.ApplyLiveChanges(new(Volume: cfg.Volume, Pitch: cfg.Pitch));
        }

        public void Dispose()
        {
            AvsSound.Dispose();
        }
    }
}