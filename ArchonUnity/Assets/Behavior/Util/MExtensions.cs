using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MExtensions
{
    public static Vector3 Combine(this Vector3 a, Vector3 b, Func<float, float, float> combiner)
    {
        return M.V3(
            combiner(a.x, b.x),
            combiner(a.y, b.y),
            combiner(a.z, b.z)
            );
    }
    public static Vector3 Combine(this Vector3 a, Vector3 b, Vector3 c, Func<float, float, float, float> combiner)
    {
        return M.V3(
            combiner(a.x, b.x, c.x),
            combiner(a.y, b.y, c.y),
            combiner(a.z, b.z, c.z)
            );
    }
}