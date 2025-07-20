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

        /// <summary>
        /// Calculates the effective width of the texture based on the rectangle and the texture's width.
        /// </summary>
        public float EffectiveWidth => Rect.width * (Texture?.width ?? 0);
        /// <summary>
        /// Gets the effective height of the element, calculated as the product of the rectangle's height and the
        /// texture's height.
        /// </summary>
        public float EffectiveHeight => Rect.height * (Texture?.height ?? 0);
        public static AtlasTexture Empty => new AtlasTexture(null, new Rect(0, 0, 0, 0));

        /// <summary>
        /// Calculates the aspect ratio of the texture based on its effective width and height.
        /// If the effective height is zero, it defaults to 1 to avoid division by zero.
        /// </summary>
        public float AspectRatio => EffectiveHeight != 0
            ? EffectiveWidth / EffectiveHeight
            : 1;

        public static AtlasTexture FromFullTexture(Texture2D texture)
        {
            if (texture == null)
                return Empty;
            return new AtlasTexture(texture, new Rect(0, 0, 1, 1));
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
