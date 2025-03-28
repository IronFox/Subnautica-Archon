using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BayControl : MonoBehaviour
{
    public float secondsToOpen = 3;
    public bool open;
    private float progress = 0;
    private new Animation animation;
    // Start is called before the first frame update
    void Start()
    {
        animation = GetComponent<Animation>();
    }

    // Update is called once per frame
    void Update()
    {
        if (open)
            progress += Time.deltaTime / secondsToOpen;
        else
            progress -= Time.deltaTime / secondsToOpen;
        progress = M.Saturate(progress);

        if (!animation.isPlaying)
            animation.Play();
        foreach (AnimationState state in animation)
        {
            state.normalizedTime = progress;
        }
    }
}
