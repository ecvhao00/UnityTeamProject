using UnityEngine;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "NewMainScene";

    private void Start()
    {
        SceneFader.FadeIn();
        ShowOpImage();
    }

    private void Update()
    {
        if (SceneFader.IsTransitioning) return;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            SceneFader.LoadScene(mainSceneName);
        }
    }

    private void ShowOpImage()
    {
        Transform canvas = RuntimeUiUtility.CreateOverlayCanvas("Title Canvas").transform;

        GameObject imageObject = new("OP");
        imageObject.transform.SetParent(canvas, false);

        Image image = imageObject.AddComponent<Image>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("OP");
        image.sprite = sprites.Length > 0 ? sprites[0] : null;
        image.preserveAspect = true;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
