using UnityEngine;

public class InteriorLightAdapt : MonoBehaviour, ILightListener
{
    private Light target;
    private float intensity;
	private float range;
	public int priority;

	public void SetExteriorLightStrip(Color stripColor) { }

    public void SetInteriorLight(Color lightColor, Color stripColor, int minimumInteriorLightPriority)
    {
        if (target)
        {
			if (minimumInteriorLightPriority <= priority)
			{
                target.color = lightColor;
				target.enabled = true;
				target.range = range * (1 + 0.2f * minimumInteriorLightPriority);
			}
			else
				target.enabled = false;
		}
    }
    void Awake()
    {
        target = GetComponent<Light>();
        intensity = target.intensity;
		range = target.range;
    }
}
