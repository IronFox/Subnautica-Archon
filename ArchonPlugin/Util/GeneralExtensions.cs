using System.Collections.Generic;
using UnityEngine;

namespace Subnautica_Archon.Util
{
    public static class GeneralExtensions
    {
        public static Rect ToRect(
            this IEnumerable<Vector2> vector)
        {
            var x = Range.Empty;
            var y = Range.Empty;
            foreach (var v in vector)
            {
                x.Include(v.x);
                y.Include(v.y);
            }
            return new Rect(
                x.Min,
                y.Min,
                x.Max - x.Min,
                y.Max - y.Min
            );
        }

    }
}
