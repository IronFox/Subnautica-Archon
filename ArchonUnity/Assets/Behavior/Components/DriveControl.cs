using Assets.Behavior.Adapters;
using UnityEngine;

public class DriveControl : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform[] alwaysFullPropellers;
    public Transform[] halfPropellers;
    public Transform[] fullWhenCameraIsExternalPropellers;
    public float maxRPS = 100;
    private float initialLevel;
    private float initialPitch;
    public ParticleSystem regularParticleSystem;
    public ParticleSystem overdriveParticleSystem;
    public SoundAdapter regularAudioSource;
    public SoundAdapter overdriveAudioSource;
    public float thrust;
    public float overdrive;
    private bool cameraWasExternal;
    private float waterDensity = 0;
    private float emissionSpeed;
    private float emissionRate;
    private bool wasEverInWater;
    private ArchonControl archon;

    private Vector3 lastPosition;
    private Vector3 lastMainPosition;

    void Awake()
    {
        archon = GetComponentInParent<ArchonControl>();
        if (archon == null)
            enabled = false;
    }

    void Start()
    {
        if (regularParticleSystem != null)
        {
            emissionSpeed = regularParticleSystem.main.startSpeedMultiplier;
            emissionRate = regularParticleSystem.emission.rateOverTimeMultiplier;
            lastPosition = regularParticleSystem.transform.position;
        }
        lastMainPosition = transform.position;
        if (regularAudioSource != null)
        {
            initialLevel = regularAudioSource.volume;
            initialPitch = regularAudioSource.pitch;
        }
    }

    // Update is called once per frame
    void Update()
    {
        thrust = Mathf.Clamp(thrust, -1, 1);

        if (archon.outOfWater)
            waterDensity -= Time.deltaTime;
        else
        {
            waterDensity += Time.deltaTime;
            wasEverInWater = true;
        }

        waterDensity = Mathf.Clamp01(waterDensity);

        bool cameraIsExternal = !archon.CameraIsInVehicle;
        if (cameraWasExternal != cameraIsExternal && wasEverInWater)
        {
            Log.Default.Write("Switching propeller visibility since vehicle is not out of water and camera is external changed");
            cameraWasExternal = cameraIsExternal;

            foreach (var p in fullWhenCameraIsExternalPropellers)
            {
                if (p != null)
                    p.gameObject.SetActive(cameraIsExternal);
            }
            foreach (var p in halfPropellers)
            {
                if (p != null)
                    p.gameObject.SetActive(!cameraIsExternal);
            }
        }
        if (Time.deltaTime > 0)
        {
            var speed = -Vector3.Dot(transform.position - lastMainPosition, transform.forward) / Time.deltaTime;
            speed *= waterDensity;
            float rot = maxRPS * (speed / 10 + thrust) * Time.deltaTime;
            foreach (var p in alwaysFullPropellers)
            {
                Rotate(p, rot);
            }
            foreach (var p in fullWhenCameraIsExternalPropellers)
            {
                Rotate(p, rot);
            }
        }


        if (regularAudioSource != null)
        {
            if (Time.deltaTime > 0)
            {
                float speed = (transform.position - lastMainPosition).magnitude / Time.deltaTime;
                speed *= waterDensity;
                if (!archon.IsBeingControlled)
                    speed = 0;
                

                var audioThrust = speed / 30;
                //Log.Write($"DriveControl.Update: Speed: {speed} -> {audioThrust}");
                //if (audioThrust > 0)
                {
                    regularAudioSource.volume = initialLevel * (0.1f + 0.9f * audioThrust);
                    regularAudioSource.pitch = initialPitch * (1 + audioThrust * 0.5f);
                    //regularAudioSource.enabled = true;
                }
            }
            //else
            //  regularAudioSource.enabled = false;
        }

        if (overdrive > 0)
        {
            if (overdriveAudioSource != null)
            {
                //overdriveAudioSource.volume = overdrive;
                //overdriveAudioSource.pitch = 0.5f + overdrive;
                //overdriveAudioSource.enabled = true;
            }
            if (overdriveParticleSystem != null)
            {
                var em = overdriveParticleSystem.emission;
                em.enabled = true;
                var main = overdriveParticleSystem.main;
                main.startSize = overdrive;
                main.startLifetime = 0.2f * overdrive;
            }
        }
        else
        {
            //if (overdriveAudioSource != null)
            //    overdriveAudioSource.enabled = false;
            if (overdriveParticleSystem != null)
            {
                var em = overdriveParticleSystem.emission;
                em.enabled = false;
            }
        }


        if (regularParticleSystem != null)
        {
            var inh = regularParticleSystem.inheritVelocity;
            inh.mode = ParticleSystemInheritVelocityMode.Initial;

            var module = regularParticleSystem.main;
            module.startSpeedMultiplier = emissionSpeed * thrust;

            var em = regularParticleSystem.emission;

            var velocity = regularParticleSystem.transform.position - lastPosition;

            em.enabled = thrust > 0 && Vector3.Dot(velocity, regularParticleSystem.transform.forward) < 0;
            em.rateOverTimeMultiplier = emissionRate * 5 * (M.Abs(thrust) + overdrive);

            lastPosition = regularParticleSystem.transform.position;
        }
        if (Time.deltaTime > 0)
            lastMainPosition = transform.position;
        //foreach (var p in propellers)
        //    p.Rotate(0, 0, thrust * maxRPS * Time.deltaTime);
    }

    private void Rotate(Transform propeller, float rot)
    {
        if (propeller)
            propeller.transform.localEulerAngles += new Vector3(0, 0, rot);
    }
}
