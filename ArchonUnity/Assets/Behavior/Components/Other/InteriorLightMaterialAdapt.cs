using System.Collections.Generic;
using UnityEngine;

public class InteriorLightMaterialAdapt : MonoBehaviour, ILightListener
{
    private readonly List<(int Index, Color Original)> change = new List<(int, Color)>();

    public void SetExteriorLightStrip(Color stripColor) { }

    public void SetInteriorLight(Color lightColor, Color stripColor)
    {
        var renderer = GetComponent<Renderer>();
        var v = M.ScaleRGB(stripColor, 1.4f);
        foreach (var c in change)
        {
            var m = renderer.materials[c.Index];
            m.SetColor("_EmissionColor", v * c.Original);
        }
    }

    void Awake()
    {
        var renderer = GetComponent<Renderer>();
        for (int i = 0; i < renderer.materials.Length; i++)
        {
            Material mat = renderer.materials[i];
            if (mat.name.Contains("Glow"))
                change.Add((i, mat.GetColor("_EmissionColor")));
        }
    }

}
