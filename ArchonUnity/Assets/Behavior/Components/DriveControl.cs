using UnityEngine;

public class DriveControl : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform[] propellers;
    public float maxRPS = 100;
    private float initialLevel;
    private float initialPitch;
    public ParticleSystem regularParticleSystem;
    public ParticleSystem overdriveParticleSystem;
    public SoundAdapter regularAudioSource;
    public SoundAdapter overdriveAudioSource;
    public float thrust;
    public float overdrive;


    private float emissionSpeed;
    private float emissionRate;

    private Vector3 lastPosition;
    private Vector3 lastMainPosition;

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

        if (regularAudioSource != null)
        {
            if (Time.deltaTime > 0)
            {
                float speed = (transform.position - lastMainPosition).magnitude / Time.deltaTime;

                var audioThrust = speed / 30;
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
}
