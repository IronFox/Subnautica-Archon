using UnityEngine;

[ExecuteInEditMode]
public class ToggleMeshRenderer : MonoBehaviour
{
    public Material reconstructionMaterial;
    public bool requireMeshRenderer = true;

    private bool? hasMeshRenderer = null;

    // Update is called once per frame
    void Update()
    {
        if (requireMeshRenderer != hasMeshRenderer)
        {
            hasMeshRenderer = requireMeshRenderer;
            var mrs = GetComponentsInChildren<MeshRenderer>(true);
            if (!requireMeshRenderer)
            {
                foreach (var mr in mrs)
                {
                    DestroyImmediate(mr);
                }
            }
            else
            {
                var mfs = GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in mfs)
                {
                    var r = mf.GetComponent<MeshRenderer>();
                    if (r != null)
                    {
                        // If a MeshRenderer already exists, skip creating a new one
                        continue;
                    }
                    r = mf.gameObject.AddComponent<MeshRenderer>();
                    r.material = reconstructionMaterial;
                }

            }
        }
    }
}
