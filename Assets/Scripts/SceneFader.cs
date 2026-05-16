using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneFader : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.75f;

    private static SceneFader instance;

    private CanvasGroup canvasGroup;
    private bool isTransitioning;

    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void FadeIn()
    {
        SceneFader fader = EnsureInstance();
        if (fader.isTransitioning) return;

        fader.StopAllCoroutines();
        fader.StartCoroutine(fader.FadeTo(0f));
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        SceneFader fader = EnsureInstance();
        if (fader.isTransitioning) return;

        fader.StopAllCoroutines();
        fader.StartCoroutine(fader.LoadSceneRoutine(sceneName));
    }

    private static SceneFader EnsureInstance()
    {
        if (instance != null) return instance;

        GameObject faderObject = new("Scene Fader");
        instance = faderObject.AddComponent<SceneFader>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        canvasGroup.alpha = 1f;
        StartCoroutine(FadeTo(0f));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isTransitioning = true;

        yield return FadeTo(1f);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (loadOperation != null && !loadOperation.isDone)
        {
            yield return null;
        }

        yield return FadeTo(0f);

        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        EnsureUi();

        canvasGroup.blocksRaycasts = true;

        float startAlpha = canvasGroup.alpha;
        if (Mathf.Approximately(startAlpha, targetAlpha))
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = targetAlpha > 0f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;
    }

    private void EnsureUi()
    {
        if (canvasGroup != null) return;

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        Transform existingPanel = transform.Find("Fade Panel");
        GameObject panelObject = existingPanel != null ? existingPanel.gameObject : new GameObject("Fade Panel");
        panelObject.transform.SetParent(transform, false);

        Image image = panelObject.GetComponent<Image>();
        if (image == null)
        {
            image = panelObject.AddComponent<Image>();
        }

        image.color = Color.black;
        image.raycastTarget = true;

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
