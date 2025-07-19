using System;
using UnityEngine;

namespace Assets.Behavior.TransferTypes
{

    /// <summary>
    /// A texture that may be part of an atlas.
    /// </summary>
    public readonly struct AtlasTexture : IEquatable<AtlasTexture>
    {
        /// <summary>
        /// The texture being used. May be null if the texture is not available.
        /// </summary>
        public Texture2D Texture { get; }
        /// <summary>
        /// The rectangle within the texture that contains the relevant part of the final texture.
        /// Must be in the range [0, 1] for width, height, x, and y.
        /// </summary>
        public Rect Rect { get; }

        /// <summary>
        /// True if the texture exists and has a valid rectangle size.
        /// </summary>
        public bool Exists => Texture != null && Rect.width > 0 && Rect.height > 0;



        public static AtlasTexture Empty => new AtlasTexture(null, new Rect(0, 0, 0, 0));
        public static AtlasTexture FromFullTexture(Texture2D texture)
        {
            if (texture == null)
                return Empty;
            float h = 1f / Math.Min(1f, texture.height / (float)texture.width);
            float w = 1f / Math.Min(1f, texture.width / (float)texture.height);
            return new AtlasTexture(texture, new Rect(0.5f - w / 2, 0.5f - h / 2, w, h));
        }

        public AtlasTexture(Texture2D texture, Rect rect)
        {
            Texture = texture;
            Rect = rect;
        }

        public override bool Equals(object obj)
            => obj is AtlasTexture t && Equals(t);

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (Texture?.GetHashCode() ?? 0);
            hash = hash * 23 + Rect.GetHashCode();
            return hash;
        }

        public bool Equals(AtlasTexture other)
            => Texture == other.Texture && Rect == other.Rect;

        public static bool operator ==(AtlasTexture a, AtlasTexture b)
            => a.Equals(b);
        public static bool operator !=(AtlasTexture a, AtlasTexture b)
            => !(a == b);
    }

}
