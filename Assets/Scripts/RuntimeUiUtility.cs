using UnityEngine;
using UnityEngine.UI;

public static class RuntimeUiUtility
{
    public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
    {
        return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
    }

    public static Canvas CreateOverlayCanvas(string name, int sortingOrder = 0)
    {
        return SetupOverlayCanvas(new GameObject(name), sortingOrder);
    }

    public static Canvas SetupOverlayCanvas(GameObject canvasObject, int sortingOrder = 0)
    {
        Canvas canvas = canvasObject.GetOrAdd<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetOrAdd<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.GetOrAdd<GraphicRaycaster>();
        return canvas;
    }

    public static Text GetOrCreateText(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetOrAdd<Text>();        
        Font font = Resources.Load<Font>("Fonts/DOSGOTHIC");

        text.color = Color.white;
        text.raycastTarget = false;
        text.font = font;
        return text;
    }

    public static Text CreateText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        Text text = GetOrCreateText(parent, name);
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        return text;
    }

    public static Text CreateCenteredText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        FontStyle fontStyle,
        Vector2 position,
        Vector2 size)
    {
        Text text = CreateText(parent, name, value, fontSize, fontStyle);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    public static CanvasGroup SetupCanvasGroup(GameObject gameObject, bool blocksRaycasts)
    {
        CanvasGroup canvasGroup = gameObject.GetOrAdd<CanvasGroup>();
        canvasGroup.blocksRaycasts = blocksRaycasts;
        canvasGroup.interactable = false;
        return canvasGroup;
    }
}
