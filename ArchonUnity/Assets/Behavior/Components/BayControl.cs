using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BayControl : MonoBehaviour
{
    public float secondsToOpen = 3;
    public bool open;
    private float progress = 0;
    private new Animation animation;
    public Transform bayInsides;
    // Start is called before the first frame update
    void Start()
    {
        animation = GetComponent<Animation>();
        SetBayVisible(false);
    }

    private void SetBayVisible(bool visible)
    {
        foreach (var r in bayInsides.GetComponentsInChildren<Renderer>())
            r.enabled = visible;
        foreach (var r in bayInsides.GetComponentsInChildren<Light>())
            r.enabled = visible;
    }

    // Update is called once per frame
    void Update()
    {
        var wasClosed = progress == 0;
        if (open)
            progress += Time.deltaTime / secondsToOpen;
        else
            progress -= Time.deltaTime / secondsToOpen;


        progress = M.Saturate(progress);
        var nowClosed = progress == 0;

        if (wasClosed && nowClosed)
        {
            animation.Stop();
            return;
        }
        if (wasClosed != nowClosed)
        {
            SetBayVisible(!nowClosed);
        }


        if (!animation.isPlaying)
            animation.Play();
        foreach (AnimationState state in animation)
        {
            state.normalizedTime = progress;
        }

        if (progress == 0 || progress == 1)
        {
            animation.Stop();
        }
    }
}
