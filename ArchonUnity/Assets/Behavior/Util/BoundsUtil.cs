using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BoundsUtil
{
    public static bool Contains(this Bounds big, Bounds small)
    {
        return small.min.GreaterOrEqual(big.min).All
            && small.max.LessOrEqual(big.max).All;
    }

    public static Bounds TranslateBy(this Bounds b, Vector3 delta)
        => new Bounds(b.center + delta, b.size);


    public static IEnumerable<Vector3> GetCornerPoints(this Bounds bounds)
    {
        yield return M.V3( bounds.min.x, bounds.min.y, bounds.min.z );
        yield return M.V3( bounds.max.x, bounds.min.y, bounds.min.z );
        yield return M.V3( bounds.min.x, bounds.max.y, bounds.min.z );
        yield return M.V3( bounds.max.x, bounds.max.y, bounds.min.z );
        yield return M.V3( bounds.min.x, bounds.min.y, bounds.max.z );
        yield return M.V3( bounds.max.x, bounds.min.y, bounds.max.z );
        yield return M.V3( bounds.min.x, bounds.max.y, bounds.max.z );
        yield return M.V3( bounds.max.x, bounds.max.y, bounds.max.z );
    }
    
    public static IEnumerable<Vector3> GetCornerPoints(this Collider c)
    {
        return GetCornerPoints(c.bounds);
    }



    public static Bounds ComputeScaledLocalRendererBounds(this Transform rootTransform)
    {
        Bounds bounds = new Bounds(Vector3.zero,M.V3(0));
        rootTransform
            .GetComponentsInChildren<Renderer>()
            .Where(x => x.enabled)
            .SelectMany(x => x.bounds.GetCornerPoints())
            .Select(x => rootTransform.InverseTransformPoint(x))
            .Select(x => M.Mult(x, rootTransform.localScale))
            .ForEach(x => bounds.Encapsulate(x));

        return bounds;
    }
    public static Bounds ComputeScaledLocalColliderBounds(this Transform rootTransform)
    {
        Bounds bounds = new Bounds(Vector3.zero,M.V3(0));
        rootTransform
            .GetComponentsInChildren<Collider>()
            .Where(x => x.enabled)
            .SelectMany(GetCornerPoints)
            .Select(rootTransform.InverseTransformPoint)
            .Select(x => M.Mult(x, rootTransform.localScale))
            .ForEach(x => bounds.Encapsulate(x));

        return bounds;
    }
}