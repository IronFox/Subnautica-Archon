using UnityEngine;

public class InteriorLightAdapt : MonoBehaviour, IInteriorLightListener
{
    private Light target;
    private float intensity;
    public void SetInteriorLight(float strength)
    {
        if (target)
            target.intensity = intensity * strength;
    }
    void Awake()
    {
        target = GetComponent<Light>();
        intensity = target.intensity;
    }
}
