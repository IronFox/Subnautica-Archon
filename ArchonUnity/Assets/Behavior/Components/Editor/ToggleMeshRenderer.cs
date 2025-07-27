using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class ToggleMeshRenderer : MonoBehaviour
{
    public Material reconstructionMaterial;
    public bool requireMeshRenderer = true;

    private bool? hasMeshRenderer = null;

    void Awake()
    {
        enabled = false;
    }

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
                    if (mr.transform.name == "Visualization")
                    {
                        DestroyImmediate(mr.gameObject);
                        continue;
                    }
                    DestroyImmediate(mr);
                }
            }
            else
            {
                if (reconstructionMaterial == null)
                    reconstructionMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
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

                var boxColliders = GetComponentsInChildren<BoxCollider>(true);
                foreach (var bc in boxColliders)
                {
                    if (bc.GetComponent<MeshFilter>() == null)
                    {
                        var childTransform = bc.transform.Find("Visualization");
                        if (childTransform != null)
                        {
                            // If a child named "Visualization" already exists, skip creating a new one
                            continue;
                        }

                        var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        child.transform.SetParent(bc.transform, false);
                        child.transform.localPosition = bc.center;
                        child.transform.localScale = bc.size;
                        child.transform.localRotation = Quaternion.identity;
                        child.name = "Visualization";
                        child.GetComponent<MeshRenderer>().material = reconstructionMaterial;
                        DestroyImmediate(child.GetComponent<BoxCollider>());
                    }
                }

            }
        }
    }
}
