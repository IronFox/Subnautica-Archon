using Assets.Behavior.Adapters;
using Assets.Behavior.Util.Math;
using System.Collections.Generic;
using UnityEngine;

public class MapControl : MonoBehaviour
{
    public Transform display;
    public Transform displayWorld;
    public Transform displayShip;
    public Transform cameraSpace;
    public RotateCamera cameraOrientation;
    public Material worldMaterial;
    public float upClip = 0.253f;
    public float downClip = -2.02f;

    public enum OrientationMode
    {
        Unchanged,
        AsMap,
        AsMapPointNorth
    }

    public OrientationMode worldOrientation = OrientationMode.AsMapPointNorth;

    public ArchonControl archon;
    //public float worldRadius;
    public float effectiveMapRadius = 4.09f;

    public float WorldRadius => effectiveMapRadius / display.localScale.x;
    private List<Renderer> mapRenderers = new List<Renderer>();
    // Start is called before the first frame update
    void Start()
    {
        RedetectRenderers();
    }

    public void RedetectRenderers()
    {
        mapRenderers.Clear();
        displayWorld.GetComponentsInChildren(mapRenderers);
    }

    // Update is called once per frame
    public void LateUpdate()
    {
        using (var log = Log.NewLazy())
        {

            switch (worldOrientation)
            {
                case OrientationMode.Unchanged:
                    break;
                case OrientationMode.AsMap:
                    displayWorld.localRotation = Quaternion.identity;
                    break;
                case OrientationMode.AsMapPointNorth:
                    displayWorld.localRotation =
                            Quaternion.Euler(0, -transform.rotation.eulerAngles.y, 0);
                    break;
            }
            if (cameraOrientation)
            {
                cameraSpace.localRotation = Quaternion.Euler(0, cameraOrientation.transform.rotation.eulerAngles.y - transform.rotation.eulerAngles.y, 0);
            }
            displayShip.localRotation = Quaternion.Euler(transform.eulerAngles.x, 0, transform.eulerAngles.z);

            RedetectRenderers();
            foreach (var renderer in mapRenderers)
            {
                var materials = renderer.materials;
                renderer.gameObject.layer = 8;
                //renderer.renderingLayerMask = 27;
                var matrix = display.worldToLocalMatrix * renderer.localToWorldMatrix;
                var localMatrix = renderer.transform.ToLocalMatrix();
                for (int i = 0; i < materials.Length; i++)
                {
                    var m = materials[i];
                    if (m.shader == worldMaterial.shader)
                    {
                        m.SetFloat("_DisplayScale", display.localScale.x);
                        m.SetVector("_ArchonCenterWorldPos", archon.transform.position);
                        m.SetFloat("_UpClip", upClip);
                        m.SetFloat("_DownClip", downClip);
                        m.SetFloat("_MapSize", effectiveMapRadius);
                        m.SetMatrix("_ObjectToDisplay", matrix);
                        m.SetMatrix("_LocalObject", localMatrix);

                    }
                    else
                        log.Warn($"Unexpected material shader {m.shader.name} on renderer {renderer.name} #{i} in MapControl");
                }
            }
        }
    }
}
