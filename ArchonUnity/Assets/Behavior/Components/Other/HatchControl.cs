using UnityEngine;

public class HatchControl : MonoBehaviour
{
    public PlayerDetector proximity;
    public PlayerDetector closeProximity;
    private Animation hatchAnimation;
    public float progress = 0;
    public float openSeconds = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        hatchAnimation = GetComponent<Animation>();
    }

    // Update is called once per frame
    void Update()
    {
        bool play = false;
        if (closeProximity.hasPlayer)
        {
            if (progress < 1.0f)
            {
                progress = 1;
                play = true;
            }
        }
        else if (proximity.hasPlayer)
        {
            if (progress < 1f)
            {
                progress += Time.deltaTime / openSeconds;
                progress = Mathf.Clamp01(progress);
                play = true;
            }
        }
        else
        {
            if (progress > 0.0f)
            {
                progress -= Time.deltaTime / openSeconds;
                progress = Mathf.Clamp01(progress);
                play = true;
            }
        }
        if (hatchAnimation.isPlaying != play)
            if (play)
                hatchAnimation.Play();
            else
                hatchAnimation.Stop();
        foreach (AnimationState state in hatchAnimation)
            state.normalizedTime = progress;

    }


}
