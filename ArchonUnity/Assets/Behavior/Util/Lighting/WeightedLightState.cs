using System.Collections.Generic;
using UnityEngine;

namespace Assets.Behavior.Util.Lighting
{

    public readonly struct WeightedLightState
    {
        public float InteriorLightsEnabled { get; } // 0 to 1
        public float InteriorLightScale { get; }//unclamped
        public float IsDead { get; } // 0 to 1
        public float IsCharging { get; } // 0 to 1
        public WeightedLightState(float interiorLightsEnabled = 0, float interiorLightScale = 0, float isDead = 0, float isCharging = 0)
        {
            InteriorLightsEnabled = interiorLightsEnabled;
            InteriorLightScale = interiorLightScale;
            IsDead = isDead;
            IsCharging = isCharging;
        }
    }

    internal readonly struct CapturedLightState
    {
        public bool InteriorLightsEnabled { get; }
        public float InteriorLightScale { get; }
        public bool IsDead { get; }
        public bool IsCharging { get; }
        public float Time { get; }

        public CapturedLightState(bool interiorLightsEnabled, float interiorLightScale, bool isDead, bool isCharging)
        {
            InteriorLightsEnabled = interiorLightsEnabled;
            InteriorLightScale = interiorLightScale;
            IsDead = isDead;
            IsCharging = isCharging;
            Time = UnityEngine.Time.time;
        }
    }
    internal class LightStateAccumulator
    {
        private Queue<CapturedLightState> States { get; } = new Queue<CapturedLightState>();
        public bool Add(CapturedLightState state)
        {
            if (Time.deltaTime == 0)
                return false;
            while (States.Count > 0 && Time.time - States.Peek().Time > 1f)
                States.Dequeue();
            States.Enqueue(state);
            return true;
        }

        public WeightedLightState Average
        {
            get
            {
                if (States.Count == 0)
                    return new WeightedLightState();


                float interiorLightsEnabled = 0;
                float interiorLightScale = 0;
                float isDead = 0;
                float isCharging = 0;
                float time = Time.time;
                foreach (var s in States)
                {
                    interiorLightsEnabled += s.InteriorLightsEnabled ? 1 : 0;
                    interiorLightScale += s.InteriorLightScale;
                    isDead += s.IsDead ? 1 : 0;
                    isCharging += s.IsCharging ? 1 : 0;
                }
                float count = States.Count;
                return new WeightedLightState(
                    interiorLightsEnabled: interiorLightsEnabled / count,
                    interiorLightScale: interiorLightScale / count,
                    isDead: isDead / count,
                    isCharging: isCharging / count);
            }
        }
    }
}
