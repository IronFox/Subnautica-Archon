using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class Replicator : MonoBehaviour
{
    public Material[] replicaMaterials;
    // Start is called before the first frame update
    public string objectNamePrefix = "";
    public bool redo = false;
    public GameObject prototype;
    public bool started = false;
    void Start()
    {
        started = true;
    }
    // Update is called once per frame
    void Update()
    {
        if (started)
            return;
        if (redo)
        {
            redo = false;
            var anyNot = false;
            foreach (Transform child in transform.GetChildren().ToList())
                if (child.name.StartsWith("Replica_"))
                {
                    child.parent = null;
                    DestroyImmediate(child.gameObject);
                }
                else
                {
                    Debug.LogWarning("Replicator: Child object " + child.name + " does not start with Replica_ and will not be destroyed.");
                    anyNot = true;
                }
            if (!anyNot && transform.childCount > 0)
                Debug.LogWarning("Replicator: All child objects were destroyed, but there are still children in the transform. This may be due to the object not starting with Replica_.");

            for (int i = 0; i < replicaMaterials.Length; i++)
            {
                var newReplica = Instantiate(prototype, transform);
                newReplica.name = "Replica_" + i;
                var mat = replicaMaterials[i];
                var mr = newReplica.GetComponentsInChildren<MeshRenderer>();
                //var most = mr.Max(x => x.sharedMaterials.Length);
                //var mats = new Material[most];

                foreach (var m in mr)
                {
                    var mats = new Material[m.sharedMaterials.Length];
                    for (int j = 0; j < mats.Length; j++)
                    {
                        mats[j] = mat;
                    }
                    m.sharedMaterials = mats;
                    m.transform.gameObject.name = objectNamePrefix + " " + m.name;
                }

            }
        }
    }
}
