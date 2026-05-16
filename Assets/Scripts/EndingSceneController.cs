using UnityEngine;
using UnityEngine.UI;

public class EndingSceneController : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string endingText = "The End";
    [SerializeField] private string promptText = "Press Enter";

    private void Start()
    {
        SceneFader.FadeIn();
        CreateUi();
    }

    private void Update()
    {
        if (SceneFader.IsTransitioning) return;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.R) ||
            Input.GetMouseButtonDown(0))
        {
            SceneFader.LoadScene(titleSceneName);
        }
    }

    private void CreateUi()
    {
        Canvas canvas = CreateCanvas("Ending Canvas");

        Text ending = CreateText(canvas.transform, "Ending", endingText, 72, FontStyle.Bold);
        RectTransform endingRect = ending.GetComponent<RectTransform>();
        endingRect.anchorMin = new Vector2(0f, 0.5f);
        endingRect.anchorMax = new Vector2(1f, 0.5f);
        endingRect.pivot = new Vector2(0.5f, 0.5f);
        endingRect.anchoredPosition = new Vector2(0f, 72f);
        endingRect.sizeDelta = new Vector2(0f, 120f);

        Text prompt = CreateText(canvas.transform, "Prompt", promptText, 32, FontStyle.Normal);
        RectTransform promptRect = prompt.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0f, 0.5f);
        promptRect.anchorMax = new Vector2(1f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = new Vector2(0f, -52f);
        promptRect.sizeDelta = new Vector2(0f, 72f);
    }

    private static Canvas CreateCanvas(string canvasName)
    {
        GameObject canvasObject = new(canvasName);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);

        Text textComponent = textObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;

        return textComponent;
    }
}
