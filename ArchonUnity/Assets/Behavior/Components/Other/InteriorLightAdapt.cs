using UnityEngine;

public class InteriorLightAdapt : MonoBehaviour, ILightListener
{
    private Light target;
    private float intensity;

    public void SetExteriorLightStrip(Color stripColor) { }

    public void SetInteriorLight(Color lightColor, Color stripColor)
    {
        if (target)
        {
            target.color = lightColor;
            target.intensity = 1;
        }
    }
    void Awake()
    {
        target = GetComponent<Light>();
        intensity = target.intensity;
    }
}
