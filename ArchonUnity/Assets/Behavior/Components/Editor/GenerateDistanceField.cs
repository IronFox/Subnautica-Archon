using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Helper behaviours to generate a 3D distance field texture based on the specified bounds and colliders found in or as children of the local transform.
/// If <see cref="bounds"/> is correctly set to the vehicle root, all other colliders are disabled before updating the texture.
/// </summary>
[ExecuteInEditMode]
public class GenerateDistanceField : MonoBehaviour
{
	public Texture3D visualizationTexture;
	public float pixelsPerUnit = 10;

	public MeshCollider[] exclude;
	public float bias = -0.1f;
	public Transform root;
	/// <summary>
	/// The minimal bounding box of all included colliders. Updated when <see cref="GenerateTexture"/> is called.
	/// </summary>
	public Bounds bounds;

	public long totalTexels;
	public long totalBytes;

	public Color visualizationBoxColor = new Color(0.3f, 0.3f, 0f, 0.2f);
	public Color visualizationVolumeColor = new Color(0.3f, 0.4f, 1f, 0.8f);

	[Header("Editor visualization")] public bool visualizeInEditor;
	[Tooltip("Percentage value representing the depth through the volume at which the cross section is visualized.")]
	[Range(0f, 1f)]
	public float crossSectionVisualizationDepth;

	[Tooltip("The visual cross section will be perpendicular to this axis.")]
	public Axis crossSectionVisualizationAxis;
	[Tooltip("If false (default value), the interior pixels will be rendered. If true, exterior pixels are rendered.")]
	public bool showUnoccupied;

	public Vector3Int resolution;


	public void OnInspectorGUI()
	{
		if (GUILayout.Button("Your blah"))
		{
		}
	}




#if UNITY_EDITOR

	[Button(nameof(UpdateTexture))]
	public bool rebuild1;

	public void UpdateTexture()
	{
		GenerateTexture(ref visualizationTexture, out bounds);
	}

	[Button(nameof(ExportTexture))]
	public bool rebuild2;

	public void ExportTexture()
	{

		if (transform.parent.position != Vector3.zero
			|| transform.parent.rotation != Quaternion.identity
		)
		{
			Debug.LogError("GenerateDistanceField: Exporting distance field texture, but the parent transform is not at the origin. This may lead to unexpected results.");
			return;
		}

		Texture3D existing = AssetDatabase.LoadAssetAtPath<Texture3D>("Assets/distanceField.asset");
		Texture3D t = null;
		GenerateTexture(ref t, out var bounds);
		t.name = "DistanceField";
		if (existing)
			EditorUtility.CopySerialized(t, existing);
		else
			AssetDatabase.CreateAsset(t, $"Assets/distanceField.asset");
		AssetDatabase.SaveAssets();

		Debug.LogWarning($"Created distanceField.asset");
		Debug.LogWarning("Center: " + bounds.center);
		Debug.LogWarning("Size: " + bounds.size);

		var distanceFieldMeta = GetComponent<DistanceFieldMeta>();
		if (distanceFieldMeta)
		{
			distanceFieldMeta.localBounds = bounds;
			//distanceFieldMeta.texture = t;
		}
	}

	void Update()
    {


    }
#endif

    public Bounds ComputeBoundingBox()
    {
        bounds = new Bounds();
        bounds.SetMinMax(M.V3(float.MaxValue, float.MaxValue, float.MaxValue), M.V3(float.MinValue, float.MinValue, float.MinValue));
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
            bounds.Encapsulate(collider.bounds);
        }
        return bounds;
    }



    public void GenerateTexture(ref Texture3D target, out Bounds bounds)
    {
        if (root == null)
            throw new InvalidOperationException("Bounds or root transform is not set.");

        bounds = new Bounds();
        bounds.SetMinMax(M.V3(float.MaxValue, float.MaxValue, float.MaxValue), M.V3(float.MinValue, float.MinValue, float.MinValue));


        //var disable = root.GetAllColliders(gameObject).ToArray();
        //List<Collider> disableList = new List<Collider>(disable);
        //foreach (var collider in disable)
        //{
        //	//if (collider.isTrigger)
        //	//	continue;
        //	if (collider.enabled)
        //	{
        //		Debug.LogWarning("GenerateDistanceField: Collider " + collider.name + " is enabled, disabling it for distance field generation.");
        //		collider.enabled = false;
        //		disableList.Add(collider);
        //	}
        //}
        var myOriginalColliders = GetComponentsInChildren<MeshCollider>();
        var temporary = new List<GameObject>();
        var myMeshes = new List<Mesh>();
        var myColliders = new List<MeshCollider>();
        try
        {
            foreach (var collider in myOriginalColliders)
            {
				if (exclude == null || !exclude.Contains(collider))
                {
                    GameObject go = new GameObject();
                    temporary.Add(go);
                    go.transform.localScale = collider.transform.localScale;
                    go.transform.localPosition = transform.localPosition + collider.transform.localPosition;
                    go.transform.localRotation = collider.transform.localRotation;
                    var c = go.AddComponent<MeshCollider>();
                    myColliders.Add(c);
                    c.sharedMaterial = collider.sharedMaterial;
                    c.sharedMesh = collider.sharedMesh;
                    c.convex = true;
                    c.enabled = true;
                    Debug.Log($"GenerateDistanceField: Cloned collider {c.NiceName()} with bounds {c.bounds}");
                    bounds.Encapsulate(c.bounds);
                }
              
            }
            Debug.Log($"GenerateDistanceField: Found {myColliders.Count} colliders in {root.NiceName()} with bounds {bounds}");

            bounds.size += M.V3(20f / pixelsPerUnit);

            resolution = Vector3Int.CeilToInt(bounds.size * pixelsPerUnit); ;

            if (target == null
             || target.width != resolution.x
             || target.height != resolution.y
             || target.depth != resolution.z)
            {
                DestroyImmediate(target);
                target = new Texture3D(resolution.x, resolution.y, resolution.z, TextureFormat.Alpha8, 1);
                target.wrapMode = TextureWrapMode.Clamp;
                totalTexels = (long)target.width * target.height * target.depth;
                totalBytes = totalTexels;   //actually the same
            }

            var started = DateTime.Now;
            var boundsCenter = bounds.center;

            var maxDistance = 2f / pixelsPerUnit;   // two texels wide
            var checkBoxExtents = M.V3(0.001f);
            var grid = new Vector4[resolution.x,resolution.y,resolution.z];
            for (int z = 0; z < resolution.z; z++)
            {
                for (int y = 0; y < resolution.y; y++)
                {
                    for (int x = 0; x < resolution.x; x++)
                    {
                        var center = PixelToWorldCoordinate(boundsCenter, new Vector3(x, y, z));// lowerBounds + new Vector3(x, y, z) * (1 / pixelsPerUnit);
                        //var occupied = Physics.CheckBox(center, checkBoxExtents);

                        //if (occupied)
                        //	target.SetPixel(x, y, z, Color.clear);
                        //else
                        //{
                        float distance = float.MaxValue;
                        Vector3 closestPoint = default;
                        foreach (var c in myColliders)
                        {
                            var closest = Physics.ClosestPoint(center, c, c.transform.position, c.transform.rotation);

                            var d = M.SqrDistance(center, closest);
                            if (d < distance)
                            {
                                distance = d;
                                closestPoint = closest;
                            }
                            if (distance == 0)
                            {
                                break;
                            }
                        }
                        if (distance == 0)
                        {

                        }
                        else
                        {
                            distance = Mathf.Sqrt(distance);
                            grid[x, y, z] = M.V4(closestPoint, distance);
                        }
                        //}


                    }
                }
            }


            for (int ri = 0; ri < 4; ri++)
            {
                int updated = 0;
                DateTime started1 = DateTime.Now;
                var dontRead = new bool[resolution.x, resolution.y, resolution.z];
                for (int z = 0; z < resolution.z; z++)
                {
                    for (int y = 0; y < resolution.y; y++)
                    {
                        for (int x = 0; x < resolution.x; x++)
                        {
                            if (grid[x, y, z].w == 0)
                            {
                                Vector3 bestP = default;
                                float bestD = float.MaxValue;
                                for (int x1 = -1; x1 <= 1; x1++)
                                    for (int y1 = -1; y1 <= 1; y1++)
                                        for (int z1 = -1; z1 <= 1; z1++)
                                        {
                                            int x2 = x + x1;
                                            int y2 = y + y1;
                                            int z2 = z + z1;
                                            if (x2 < 0 || y2 < 0 || z2 < 0)
                                                continue;
                                            if (x2 >= resolution.x || y2 >= resolution.y || z2 >= resolution.z)
                                                continue;
                                            if (dontRead[x2, y2, z2])
                                                continue;
                                            var v = grid[x2, y2, z2];
                                            if (v.w == 0)
                                                continue;
                                            float d = M.SqrDistance(v, PixelToWorldCoordinate(boundsCenter, new Vector3(x2, y2, z2)));
                                            if (d < bestD)
                                            {
                                                bestD = d;
                                                bestP = v;
                                            }
                                        }
                                if (bestD < float.MaxValue)
                                {
                                    updated++;
                                    grid[x, y, z] = M.V4(bestP, -Mathf.Sqrt(bestD));
                                    dontRead[x, y, z] = true;
                                }
                            }

                        }
                    }
                }
                var elapsed1 = DateTime.Now - started1;
                Debug.Log($"Refinement {ri}/4 took {elapsed1.TotalSeconds} s. Updated {updated}");
            }

            {
                DateTime started1 = DateTime.Now;

                for (int z = 0; z < resolution.z; z++)
                {
                    for (int y = 0; y < resolution.y; y++)
                    {
                        for (int x = 0; x < resolution.x; x++)
                        {
                            var v = grid[x, y, z];
                            if (v.w == 0)
                                v.w = -10000;
							v.w += bias;
							var d = v.w;// * pixelsPerUnit;
                            //var relativeDistance = texelDistance / 4f;

                            var clamped = M.Clamp(d, -0.5f, 0.5f) + 0.5f;
                            //if (clamped < -0.99f)
                            //    clamped = 1;
                            target.SetPixel(x, y, z, new Color(1f, 1f, 1f, clamped));

                        }
                    }
                }
                var elapsed1 = DateTime.Now - started1;

                Debug.Log($"Pixel grid filled in {elapsed1.TotalMilliseconds} ms");
            }


            var elapsed = DateTime.Now - started; started = DateTime.Now;
            Debug.Log($"GenerateTexture took {elapsed.TotalSeconds} s");

            target.Apply();
            elapsed = DateTime.Now - started;
            Debug.Log($"texture.Apply took {elapsed.TotalMilliseconds} ms");

        }
        finally
        {
            foreach (var collider in temporary)
                DestroyImmediate(collider);
            foreach (var m in myMeshes)
                DestroyImmediate(m);
            Debug.Log("All done");
            //foreach (var collider in disableList)
            //{
            //	if (collider != null)
            //		collider.enabled = true;
            //}
        }

    }
#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!visualizeInEditor || resolution == Vector3Int.zero || visualizationTexture == null)
        {
            return;
        }
        var boundsCenter = bounds.center - bounds.size / 2 + (Vector3)resolution / 2 * (1 / pixelsPerUnit);

        Gizmos.color = visualizationBoxColor;

        Gizmos.DrawCube(boundsCenter, (Vector3)resolution / pixelsPerUnit);

        boundsCenter = bounds.center;

        Gizmos.color = visualizationVolumeColor;

        var boxSize = M.V3(1f / pixelsPerUnit);
        int a0 = (int)crossSectionVisualizationAxis;
        int a1 = (a0 + 1) % 3;
        int a2 = (a0 + 2) % 3;

        int d = Mathf.RoundToInt((resolution[a0] - 1) * crossSectionVisualizationDepth);
        for (int l = 0; l < resolution[a1]; l++)
        {
            for (int w = 0; w < resolution[a2]; w++)
            {
                var location = GetCrossSectionLocation(l, w, d, crossSectionVisualizationAxis);
                float a = visualizationTexture.GetPixel(location.x, location.y, location.z).a;
                if (!showUnoccupied)
                    a = 1f - a;
                //var transparent = Mathf.Approximately(visualizationTexture.GetPixel(location.x, location.y, location.z).a, showUnoccupied ? 1f : 0f);
                Gizmos.color = new Color(visualizationVolumeColor.r, visualizationVolumeColor.g, visualizationVolumeColor.b, a);
                //if (transparent)
                {
                    Gizmos.DrawCube(PixelToWorldCoordinate(boundsCenter, location)/*+M.V3(50)*/, boxSize);
                }
            }
        }
    }
#endif
    private static Vector3Int GetCrossSectionLocation(int length, int width, int depth, Axis axis)
    {
        switch (axis)
        {
            case Axis.Right: return new Vector3Int(depth, length, width);
            case Axis.Up: return new Vector3Int(width, depth, length);
            case Axis.Forward: return new Vector3Int(length, width, depth);
            default: return new Vector3Int();
        }
    }

    private Vector3 PixelToWorldCoordinate(Vector3 boundsCenter, Vector3 pixel) =>
        boundsCenter - bounds.size / 2 + pixel * (1 / pixelsPerUnit) + M.V3(0.5f / pixelsPerUnit);

    private static Vector3 VectorReciprocal(Vector3 original) => new Vector3(1 / original.x, 1 / original.y, 1 / original.z);

    public enum Axis
    {
        Right,
        Up,
        Forward
    }
}
