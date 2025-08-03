using UnityEngine;

public class TeleportationAnimation : MonoBehaviour
{
    public float secondsToTeleport = 100;
    private float actualProgress;
    public float opacity;
    public float progressChangeSpeed = 0.1f;
    public SoundAdapter chargeUpSound;
    public SoundAdapter teardownSound;
    private float chargeUpLen;
    private bool playingChargeUp = false;
    private bool playingTeardown = false;
    // Start is called before the first frame update
    void Awake()
    {
        chargeUpLen = chargeUpSound.clip.length;
    }

    // Update is called once per frame
    void Update()
    {
        float progress = secondsToTeleport < chargeUpLen ? (1f - secondsToTeleport / chargeUpLen) : 0;
        if (progress < actualProgress)
        {
            actualProgress -= progressChangeSpeed * Time.deltaTime;
            if (progress > actualProgress)
                actualProgress = progress;
        }
        else if (progress > actualProgress)
        {
            actualProgress += progressChangeSpeed * Time.deltaTime;
            if (progress < actualProgress)
                actualProgress = progress;
        }
        if (progress > 0 && !playingChargeUp)
        {
            playingChargeUp = true;
            chargeUpSound.Play();
        }
        opacity = 1f - Mathf.Pow(1f - actualProgress, 3f);
        if (secondsToTeleport < 0.25f && !playingTeardown)
        {
            playingTeardown = true;
            teardownSound.Play();
        }

        if (progress == 0)
        {
            playingChargeUp = false;
            playingTeardown = false;
        }
        int at = 0;
        GetComponentsInChildren<Renderer>().ForEach(r =>
        {
            r.material.SetFloat("_Opacity", opacity);
            r.material.SetVector("_Center", transform.position);
            r.material.SetFloat("_Seed", 1f + (at++) * 0.1f);
            r.transform.rotation = Quaternion.identity; //force upright

        });
    }
}
