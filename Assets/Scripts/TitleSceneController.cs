using UnityEngine;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "NewMainScene";

    private Image titleImage;
    private bool showingOp;

    private void Start()
    {
        SceneFader.FadeIn();
        CreateTitleImage();
        ShowImage("Tite");
    }

    private void Update()
    {
        if (SceneFader.IsTransitioning) return;
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;

        if (!showingOp)
        {
            showingOp = true;
            ShowImage("OP");
            return;
        }

        SceneFader.LoadScene(mainSceneName);
    }

    private void CreateTitleImage()
    {
        Transform canvas = RuntimeUiUtility.CreateOverlayCanvas("Title Canvas").transform;

        GameObject imageObject = new("Title Image");
        imageObject.transform.SetParent(canvas, false);

        titleImage = imageObject.AddComponent<Image>();
        titleImage.preserveAspect = true;
        titleImage.raycastTarget = false;

        RectTransform rect = titleImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ShowImage(string resourceName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourceName);
        titleImage.sprite = sprites.Length > 0 ? sprites[0] : null;
    }
}
