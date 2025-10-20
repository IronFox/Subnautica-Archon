using Assets.Behavior.Adapters;
using UnityEngine;

namespace Assets.Behavior.Components.Animations
{
    [RequireComponent(typeof(SoundAdapter))]
    public class Bioreactor : MonoBehaviour
    {
        internal float isCharging;
        internal bool powerOff;
        internal float soundVolume = 1;
        public SoundAdapter soundAdapter;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            soundAdapter.volume = (powerOff ? 0 : (isCharging * 0.8f + 0.2f) * soundVolume);
        }
    }

}