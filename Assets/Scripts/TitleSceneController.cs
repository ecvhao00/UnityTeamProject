using UnityEngine;
public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "NewMainScene";
    [SerializeField] private string titleText = "G4 Team Project";
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
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            SceneFader.LoadScene(mainSceneName);
        }
    }

    private void CreateUi()
    {
        Transform canvas = RuntimeUiUtility.CreateOverlayCanvas("Title Canvas").transform;
        RuntimeUiUtility.CreateCenteredText(canvas, "Title", titleText, 72, FontStyle.Bold, new Vector2(0f, 72f), new Vector2(0f, 120f));
        RuntimeUiUtility.CreateCenteredText(canvas, "Prompt", promptText, 32, FontStyle.Normal, new Vector2(0f, -52f), new Vector2(0f, 72f));
    }
}
