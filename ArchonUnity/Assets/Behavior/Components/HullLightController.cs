using UnityEngine;

public class HullLightController : MonoBehaviour
{
    public bool lightsEnabled;
    public LightShadows shadows = LightShadows.None;
    private bool lastLights;
    private LightShadows lastShadows;
    private Light[] lights;
    // Start is called before the first frame update
    void Start()
    {
        lights = GetComponentsInChildren<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        if (lastLights != lightsEnabled || shadows != lastShadows)
        {
            lastLights = lightsEnabled;
            lastShadows = shadows;
            foreach (var light in lights)
            {
                light.enabled = lightsEnabled;
                light.shadows = shadows;
            }
        }
    }
}
