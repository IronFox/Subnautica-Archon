using UnityEngine;

public class StripRenderers : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var renders = GetComponentsInChildren<Renderer>();
        foreach (var r in renders)
        {
            Destroy(r);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
