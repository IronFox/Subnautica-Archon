using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HullLightController : MonoBehaviour
{
    public bool lightsEnabled;
    private bool lastLights;
    private Light[] lights;
    // Start is called before the first frame update
    void Start()
    {
        lights = GetComponentsInChildren<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        if (lastLights != lightsEnabled)
        {
            lastLights = lightsEnabled;
            foreach (var light in lights)
                light.enabled = lightsEnabled;
        }
    }
}
