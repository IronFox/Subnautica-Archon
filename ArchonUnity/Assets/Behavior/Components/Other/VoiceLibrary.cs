using System.Collections.Generic;
using UnityEngine;

public class VoiceLibrary : MonoBehaviour
{
    public string voiceName = "Default";
    public AudioClip[] damageDetected;
    public AudioClip[] depthCriticalPost;
    public AudioClip[] depthCriticalPre;
    public AudioClip[] depthDangerous;
    public AudioClip[] emergencyRepair;
    public AudioClip[] healthCritical;
    public AudioClip[] healthLow;
    public AudioClip[] mobilitySuspended;
    public AudioClip[] mobilitySuspendedRepair;
    public AudioClip[] powerCritical;
    public AudioClip[] powerLow;
    public AudioClip[] prepareTeleport;
    public AudioClip[] teleport;
    public AudioClip[] allSystemsGreen;
    public AudioClip[] welcomePre;
    public AudioClip[] welcomePost;
    public AudioClip[] welcomeCombined;


    public AudioClip GetRandomDamageDetected()
        => GetRandom(damageDetected);
    public AudioClip GetRandomDepthDangerous()
        => GetRandom(depthDangerous);
    //public AudioClip GetRandomEmergencyRepair()
    //    => GetRandom(emergencyRepair);
    public AudioClip GetRandomHealthCritical()
        => GetRandom(healthCritical);

    public AudioClip GetRandomHealthLow()
        => GetRandom(healthLow);
    public IReadOnlyList<AudioClip> GetRandomMobilitySuspended(bool emergencyRepairing)
    {
        if (emergencyRepairing)
            return GetRandom(emergencyRepair, mobilitySuspended, mobilitySuspendedRepair, 1, out _);
        else
            return new AudioClip[] { GetRandom(mobilitySuspended) };
    }
    public AudioClip GetRandomPowerCritical()
        => GetRandom(powerCritical);
    public AudioClip GetRandomPowerLow()
        => GetRandom(powerLow);
    public AudioClip GetRandomPrepareTeleport()
        => GetRandom(prepareTeleport);
    public AudioClip GetRandomTeleport()
        => GetRandom(teleport);
    public AudioClip GetRandomAllSystemsGreen()
        => GetRandom(allSystemsGreen);


    public IReadOnlyList<AudioClip> GetRandomDepthCritical()
    {
        return GetRandom(depthCriticalPre, depthCriticalPost, null, 1, out _);
    }
    private AudioClip GetRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;
        int index = UnityEngine.Random.Range(0, clips.Length);
        return clips[index];
    }

    private IReadOnlyList<AudioClip> GetRandom(AudioClip[] pre, AudioClip[] post, AudioClip[] combined, float postProbability, out bool isCombined)
    {
        // Count possible outcomes
        int combinedCount = combined?.Length ?? 0;
        int preCount = pre?.Length ?? 0;
        int postCount = post?.Length ?? 0;
        //int pairCount = preCount * postCount;
        int totalOutcomes = combinedCount + preCount;
        if (totalOutcomes == 0)
        {
            isCombined = false;
            return null;
        }
        int choice = UnityEngine.Random.Range(0, totalOutcomes);
        if (choice < combinedCount)
        {
            // Pick from combined
            isCombined = true;
            return new AudioClip[] { combined[choice] };
        }
        else
        {
            // Pick a (pre, post) pair
            int preIndex = choice - combinedCount;
            if (UnityEngine.Random.value < postProbability)
            {
                isCombined = false;
                int postIndex = UnityEngine.Random.Range(0, postCount);
                return new AudioClip[] { pre[preIndex], post[postIndex] };
            }
            isCombined = false;
            return new AudioClip[] { pre[preIndex] };
        }
    }

    public IReadOnlyList<AudioClip> GetRandomWelcome(out bool isCombined)
    {
        return GetRandom(welcomePre, welcomePost, welcomeCombined, 0.5f, out isCombined);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
