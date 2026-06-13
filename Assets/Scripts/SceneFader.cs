using System.Collections;
using System;
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

    public static IEnumerator FadeOutIn(Action onBlack)
    {
        SceneFader fader = EnsureInstance();
        if (fader.isTransitioning) yield break;

        fader.isTransitioning = true;
        fader.StopAllCoroutines();

        yield return fader.FadeTo(1f);
        onBlack?.Invoke();
        yield return fader.FadeTo(0f);

        fader.isTransitioning = false;
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
        MainSceneBgm.FadeOut();

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

        RuntimeUiUtility.SetupOverlayCanvas(gameObject, 10000);
        canvasGroup = RuntimeUiUtility.SetupCanvasGroup(gameObject, true);

        Transform existingPanel = transform.Find("Fade Panel");
        GameObject panelObject = existingPanel != null ? existingPanel.gameObject : new GameObject("Fade Panel");
        panelObject.transform.SetParent(transform, false);

        Image image = panelObject.GetOrAdd<Image>();

        image.color = Color.black;
        image.raycastTarget = true;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
