using Assets.Behavior.TransferTypes;
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

        public static AtlasTexture ToAtlasTexture(
            this Atlas.Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return default;
            }
            return new AtlasTexture(
                sprite.texture,
                sprite.uv0.ToRect()
                );
        }
    }
}
