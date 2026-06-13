using UnityEngine;

public static class RuntimeSpriteUtility
{
    private static Sprite whiteSquareSprite;

    public static Sprite WhiteSquareSprite
    {
        get
        {
            if (whiteSquareSprite != null) return whiteSquareSprite;

            Texture2D texture = new(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            whiteSquareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f
            );
            whiteSquareSprite.hideFlags = HideFlags.HideAndDontSave;
            return whiteSquareSprite;
        }
    }
}
