using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animation targetAnimation;
    private float progress = 0;
    public float forwardSeconds = 1f;
    public float backwardSeconds = 1f;
    public bool animateTowardsEnd = false;

    public float Progress => progress;
    void Awake()
    {
        targetAnimation = GetComponent<Animation>();
        if (targetAnimation == null)
        {
            Debug.LogError("No Animation component found on this GameObject.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (targetAnimation == null)
            return;
        if (animateTowardsEnd)
        {
            progress += Time.deltaTime / forwardSeconds;
            if (progress > 1f)
                progress = 1f;
        }
        else
        {
            progress -= Time.deltaTime / backwardSeconds;
            if (progress < 0f)
                progress = 0f;
        }

        if (!targetAnimation.isPlaying)
            targetAnimation.Play();

        foreach (AnimationState state in targetAnimation)
            state.normalizedTime = progress;

    }
}
