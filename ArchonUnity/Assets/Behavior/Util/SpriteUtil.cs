

using UnityEngine;

public static class SpriteUtil {

	public static Sprite ToSprite(Texture2D texture)
	{
		return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
	}
	
}