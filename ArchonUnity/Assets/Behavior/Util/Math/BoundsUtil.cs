using Assets.Behavior.Adapters;
using Behavior.Util.Math;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

public static class BoundsUtil
{
    public static bool Contains(this Bounds big, Bounds small)
    {
        return small.min.GreaterOrEqual(big.min).All
            && small.max.LessOrEqual(big.max).All;
    }

    public static Bounds TranslatedBy(this Bounds b, Vector3 delta)
        => new Bounds(b.center + delta, b.size);


    public static IEnumerable<Vector3> GetCornerPoints(this Bounds bounds)
    {
        yield return M.V3(bounds.min.x, bounds.min.y, bounds.min.z);
        yield return M.V3(bounds.max.x, bounds.min.y, bounds.min.z);
        yield return M.V3(bounds.min.x, bounds.max.y, bounds.min.z);
        yield return M.V3(bounds.max.x, bounds.max.y, bounds.min.z);
        yield return M.V3(bounds.min.x, bounds.min.y, bounds.max.z);
        yield return M.V3(bounds.max.x, bounds.min.y, bounds.max.z);
        yield return M.V3(bounds.min.x, bounds.max.y, bounds.max.z);
        yield return M.V3(bounds.max.x, bounds.max.y, bounds.max.z);
    }

    public static IEnumerable<Vector3> GetCornerPoints(this BoxCollider c)
    {
        var s = c.size / 2;
        var p = c.center;
        yield return M.V3(p.x + s.x, p.y + s.y, p.z + s.z);
        yield return M.V3(p.x + s.x, p.y + s.y, p.z - s.z);
        yield return M.V3(p.x + s.x, p.y - s.y, p.z + s.z);
        yield return M.V3(p.x + s.x, p.y - s.y, p.z - s.z);
        yield return M.V3(p.x - s.x, p.y + s.y, p.z + s.z);
        yield return M.V3(p.x - s.x, p.y + s.y, p.z - s.z);
        yield return M.V3(p.x - s.x, p.y - s.y, p.z + s.z);
        yield return M.V3(p.x - s.x, p.y - s.y, p.z - s.z);
    }


    public static IEnumerable<Vector3> GetCornerPoints(this CapsuleCollider c)
    {


        var r = c.radius;
        var p = c.center;
        var h = c.height / 2 + r;
        switch (c.direction)
        {
            case 0:
                yield return M.V3(p.x + h, p.y + r, p.z + r);
                yield return M.V3(p.x + h, p.y + r, p.z - r);
                yield return M.V3(p.x + h, p.y - r, p.z + r);
                yield return M.V3(p.x + h, p.y - r, p.z - r);
                yield return M.V3(p.x - h, p.y + r, p.z + r);
                yield return M.V3(p.x - h, p.y + r, p.z - r);
                yield return M.V3(p.x - h, p.y - r, p.z + r);
                yield return M.V3(p.x - h, p.y - r, p.z - r);
                break;
            case 1:
                yield return M.V3(p.x + r, p.y + h, p.z + r);
                yield return M.V3(p.x + r, p.y + h, p.z - r);
                yield return M.V3(p.x + r, p.y - h, p.z + r);
                yield return M.V3(p.x + r, p.y - h, p.z - r);
                yield return M.V3(p.x - r, p.y + h, p.z + r);
                yield return M.V3(p.x - r, p.y + h, p.z - r);
                yield return M.V3(p.x - r, p.y - h, p.z + r);
                yield return M.V3(p.x - r, p.y - h, p.z - r);
                break;
            case 2:
                yield return M.V3(p.x + r, p.y + r, p.z + h);
                yield return M.V3(p.x + r, p.y + r, p.z - h);
                yield return M.V3(p.x + r, p.y - r, p.z + h);
                yield return M.V3(p.x + r, p.y - r, p.z - h);
                yield return M.V3(p.x - r, p.y + r, p.z + h);
                yield return M.V3(p.x - r, p.y + r, p.z - h);
                yield return M.V3(p.x - r, p.y - r, p.z + h);
                yield return M.V3(p.x - r, p.y - r, p.z - h);
                break;

        }
    }



    //public static IEnumerable<Vector3> GetCornerPoints(this Collider c)
    //{
    //    return GetCornerPoints(c.bounds);
    //}

    public static Matrix4x4 ToLocalMatrix(this Transform t)
    {
        return Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
    }

    public static bool IsTooBig(Bounds bounds)
    {
        //return bounds.size.x > 5.5f || bounds.size.y > 3.0f || bounds.size.z > 8.9f; 
        return M.MaxAxis(bounds.extents) > 10f;
    }

    private static void RecurseComputeBounds(Matrix4x4 matrixToRoot, Transform transform, ref Bounds bounds, bool includeRenderers, bool includeColliders, Transform excludeFrom)
    {
        //using (var log = new LogContext(nameof(RecurseComputeBounds), transform, includeRenderers, includeColliders))
        {
            if (transform == excludeFrom)
                return;
            string t = $"matrix {matrixToRoot}, computed from scale {transform.localScale} on {transform.NiceName()}";
            if (includeRenderers)
            {
                // var mf = transform.GetComponent<MeshFilter>();
                // var r = transform.GetComponent<Renderer>();
                // var l = transform.GetComponent<Light>();
                //
                // if (r && mf && !l && (!r.name.Contains("Volum") || !r.name.Contains("Light")) && r.enabled && mf.mesh)
                // {
                //     var wasTooBig = IsTooBig(bounds);
                //     foreach (var corner in mf.mesh.bounds.GetCornerPoints())
                //         bounds.Encapsulate(matrixToRoot * corner);
                //     if (!wasTooBig && IsTooBig(bounds))
                //         Log.Default.LogError(
                //             $"Computed bounds have gotten too large ({bounds}) after using renderer {r.NiceName()} bounds {mf.mesh.bounds} on {t}");
                // }

            }

            if (includeColliders)
            {
                var c = transform.GetComponent<Collider>();
                if (c && c.enabled && !c.isTrigger)
                {
                    //log.Write($"Including {c.NiceName()} @{c.transform.localPosition} @{matrixToRoot}");
                    var wasTooBig = IsTooBig(bounds);


                    switch (c)
                    {
                        case MeshCollider mc:
                            foreach (var corner in mc.sharedMesh.bounds.GetCornerPoints())
                                bounds.Encapsulate(matrixToRoot * M.V4(corner, 1));
                            if (!wasTooBig && IsTooBig(bounds))
                                using (var log = Log.New())
                                    log.Error(
                                        $"Computed bounds have gotten too large ({bounds}) after using collider bounds {mc.sharedMesh.bounds} on {t}");
                            break;
                        case BoxCollider box:
                            foreach (var corner in box.GetCornerPoints())
                                bounds.Encapsulate(matrixToRoot * M.V4(corner, 1));
                            if (!wasTooBig && IsTooBig(bounds))
                                using (var log = Log.New())
                                    log.Error(
                                    $"Computed bounds have gotten too large ({bounds}) after using collider box {box.center}-{box.size} on {t}");
                            break;
                        case SphereCollider sphere:
                            {

                                var center = matrixToRoot * M.V4(sphere.center, 1);
                                var radius3 = M.Abs((Vector3)(matrixToRoot * M.V4(sphere.radius, 0)));
                                bounds.Encapsulate(new Bounds(center, radius3 * 2));
                                if (!wasTooBig && IsTooBig(bounds))
                                    using (var log = Log.New())
                                        log.Error(
                                            $"Computed bounds have gotten too large ({bounds}) after using collider sphere {sphere.center} r{sphere.radius} on {t}");
                            }
                            break;
                        case CapsuleCollider capsule:
                            foreach (var corner in capsule.GetCornerPoints())
                                bounds.Encapsulate(matrixToRoot * M.V4(corner, 1));
                            if (!wasTooBig && IsTooBig(bounds))
                                using (var log = Log.New())
                                    log.Error(
                                    $"Computed bounds have gotten too large ({bounds}) after using collider capsule {capsule.center} r{capsule.radius} h{capsule.height} on {t}");
                            break;

                    }

                }
            }

            foreach (var child in transform.GetChildren())
            {
                if (!child.gameObject.activeSelf)
                    continue;

                RecurseComputeBounds(matrixToRoot * child.ToLocalMatrix(), child, ref bounds,
                    includeRenderers: includeRenderers, includeColliders: includeColliders, excludeFrom: excludeFrom);
                //.Where(x => x.enabled)
                //matrixToRoot = matrixToRoot* transform.ToLocalMatrix();
            }
        }

    }




    public static Bounds3 ComputeScaledLocalBounds(this Transform rootTransform, bool includeRenderers, bool includeColliders, [CanBeNull] Transform excludeFrom)
    {
        Bounds bounds = new Bounds(Vector3.zero, M.V3(0));

        RecurseComputeBounds(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, rootTransform.localScale), rootTransform, ref bounds, includeRenderers: includeRenderers, includeColliders: includeColliders, excludeFrom: excludeFrom);

        return Bounds3.From(bounds);
    }
    //public static Bounds ComputeScaledLocalColliderBounds(this Transform rootTransform)
    //{
    //    Bounds bounds = new Bounds(Vector3.zero,M.V3(0));
    //    rootTransform
    //        .GetComponentsInChildren<Collider>()
    //        .Where(x => x.enabled)
    //        .SelectMany(GetCornerPoints)
    //        .Select(rootTransform.InverseTransformPoint)
    //        .Select(x => M.Mult(x, rootTransform.localScale))
    //        .ForEach(x => bounds.Encapsulate(x));

    //    return bounds;
    //}
}