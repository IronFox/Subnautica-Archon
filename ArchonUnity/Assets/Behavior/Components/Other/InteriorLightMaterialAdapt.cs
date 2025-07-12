using System.Collections.Generic;
using UnityEngine;

public class InteriorLightMaterialAdapt : MonoBehaviour, IInteriorLightListener
{
    private readonly List<int> change = new List<int>();

    public void SetInteriorLight(float strength)
    {
        var renderer = GetComponent<Renderer>();
        float v = strength * 1.5f;
        foreach (var c in change)
        {
            var m = renderer.materials[c];
            m.SetColor("_EmissionColor", new Color(v, v, v, 1));
        }
    }

    void Awake()
    {
        var renderer = GetComponent<Renderer>();
        for (int i = 0; i < renderer.materials.Length; i++)
            if (renderer.materials[i].name.Contains("Glow"))
                change.Add(i);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
