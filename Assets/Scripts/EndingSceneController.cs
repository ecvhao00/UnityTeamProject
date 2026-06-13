using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingSceneController : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private float returnDelay = 5f;

    private void Start()
    {
        SceneFader.FadeIn();
        ShowEndoImage();
        StartCoroutine(ReturnToTitleRoutine());
    }

    private void ShowEndoImage()
    {
        Transform canvas = RuntimeUiUtility.CreateOverlayCanvas("Ending Canvas").transform;

        GameObject imageObject = new("Endo");
        imageObject.transform.SetParent(canvas, false);

        Image image = imageObject.AddComponent<Image>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Endo");
        image.sprite = sprites.Length > 0 ? sprites[0] : null;
        image.preserveAspect = true;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator ReturnToTitleRoutine()
    {
        yield return new WaitForSecondsRealtime(returnDelay);
        SceneFader.LoadScene(titleSceneName);
    }
}
