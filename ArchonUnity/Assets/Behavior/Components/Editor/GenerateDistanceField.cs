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
	public bool updateTexture = false;
	public bool exportTexture = false;
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
	

#if UNITY_EDITOR
	void Update() {
		if (updateTexture) {
			
			updateTexture = false;
			GenerateTexture(ref visualizationTexture, out bounds);

		}

#if UNITY_EDITOR
		if (exportTexture)
		{
			exportTexture = false;

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
			Debug.LogWarning("Center: "+bounds.center);
			Debug.LogWarning("Size: "+bounds.size);

			var distanceFieldMeta = GetComponent<DistanceFieldMeta>();
			if (distanceFieldMeta)
			{
				distanceFieldMeta.localBounds = bounds;
				//distanceFieldMeta.texture = t;
			}

			//DestroyImmediate(texture);
		}
	#endif
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


		var disable = root.GetAllColliders(gameObject).ToArray();
		List<Collider> disableList = new List<Collider>(disable);
		foreach (var collider in disable)
		{
			//if (collider.isTrigger)
			//	continue;
			if (collider.enabled)
			{
				Debug.LogWarning("GenerateDistanceField: Collider " + collider.name + " is enabled, disabling it for distance field generation.");
				collider.enabled = false;
				disableList.Add(collider);
			}
		}
		try
		{
			var myColliders = GetComponentsInChildren<Collider>();
			foreach (var collider in myColliders)
			{
				collider.enabled = true;
				Debug.Log($"GenerateDistanceField: Found collider {collider.NiceName()} with bounds {collider.bounds}");
				bounds.Encapsulate(collider.bounds);
			}
			Debug.Log($"GenerateDistanceField: Found {myColliders.Length} colliders in {root.name} with bounds {bounds}");

			bounds.size += M.V3(8f / pixelsPerUnit);

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
			for (int z = 0; z < resolution.z; z++)
			{
				for (int y = 0; y < resolution.y; y++)
				{
					for (int x = 0; x < resolution.x; x++)
					{
						var center = PixelToWorldCoordinate(boundsCenter, new Vector3(x, y, z));// lowerBounds + new Vector3(x, y, z) * (1 / pixelsPerUnit);
						var occupied = Physics.CheckBox(center, checkBoxExtents);

						if (occupied)
							target.SetPixel(x, y, z, Color.clear);
						else
						{
							float distance = float.MaxValue;
							foreach (var c in myColliders)
							{
								var closest = Physics.ClosestPoint(center, c, c.transform.position, c.transform.rotation);

								var d = M.SqrDistance(center, closest);
								if (d < distance)
									distance = d;
							}
							distance = Mathf.Sqrt(distance);
							var texelDistance = distance * pixelsPerUnit;
							var relativeDistance = texelDistance / 4f;


							target.SetPixel(x, y, z, new Color(1f, 1f, 1f, Mathf.Min(relativeDistance, 1f)));
						}


					}
				}
			}
			var elapsed = DateTime.Now - started; started = DateTime.Now;
			Debug.Log($"GenerateTexture took {elapsed.TotalMilliseconds} ms");

			target.Apply();
			elapsed = DateTime.Now - started;
			Debug.Log($"texture.Apply took {elapsed.TotalMilliseconds} ms");

			foreach (var collider in disable)
			{
				collider.enabled = true;
			}
		}
		finally
		{
			foreach (var collider in disableList)
			{
				if (collider != null)
					collider.enabled = true;
			}
		}

	}
#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!visualizeInEditor || resolution == Vector3Int.zero || visualizationTexture == null)
		{
			return;
		}
		var boundsCenter = bounds.center - bounds.size / 2 + (Vector3)resolution/2 * (1 / pixelsPerUnit);

        Gizmos.color = visualizationBoxColor;

        Gizmos.DrawCube(boundsCenter, (Vector3)resolution / pixelsPerUnit);

		boundsCenter = bounds.center;

		Gizmos.color = visualizationVolumeColor;

		var boxSize = M.V3(1f / pixelsPerUnit);
		int a0 = (int)crossSectionVisualizationAxis;
		int a1 = (a0 + 1) % 3;
		int a2 = (a0 + 2) % 3;

		int d = Mathf.RoundToInt ((resolution[a0]-1) * crossSectionVisualizationDepth);
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
                    Gizmos.DrawCube(PixelToWorldCoordinate(boundsCenter, location) , boxSize);
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
		boundsCenter - bounds.size / 2 + pixel * (1 / pixelsPerUnit)+ M.V3(0.5f / pixelsPerUnit);

    private static Vector3 VectorReciprocal(Vector3 original) => new Vector3(1 / original.x, 1 / original.y, 1 / original.z);

    public enum Axis
    {
        Right,
        Up,
        Forward
    }
}
