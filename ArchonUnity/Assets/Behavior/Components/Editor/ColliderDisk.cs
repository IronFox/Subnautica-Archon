using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class ColliderDisk : MonoBehaviour
{
    public float height = 0.1f;
    public float radius = 5f;
    public int resolution = 32;
    public bool build = false;
    public bool renderers = true;
    private bool built = false;
    private bool renderersBuilt = false;



#if !UNITY_EDITOR
    public void Awake()
    {

        enabled = false;
    }
#else

    // Update is called once per frame
    public void Update()
    {
        if (build != built || renderers != renderersBuilt)
        {
            built = build;
            renderersBuilt = renderers;
            foreach (var child in transform.GetChildren().ToList())
            {
                DestroyImmediate(child.gameObject);
            }

            if (build)
            {
                float scaleZ = radius * Mathf.PI * 2f / resolution;
                for (int i = 0; i < resolution / 2; i++)
                {
                    float angle = i * 2 * Mathf.PI / resolution;
                    float x = radius * Mathf.Cos(angle);
                    float z = radius * Mathf.Sin(angle);
                    // Create a cube at the edge of the disk
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(transform, false);
                    cube.transform.localPosition = Vector3.zero;
                    cube.transform.localScale = new Vector3(radius * 2, height, scaleZ);
                    cube.transform.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
                    cube.name = "ColliderSegment_" + i;
                    if (!renderers)
                    {
                        // Disable the renderer if not needed
                        var renderer = cube.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            DestroyImmediate(renderer);
                        }
                    }
                }

            }
        }
    }
#endif
}
