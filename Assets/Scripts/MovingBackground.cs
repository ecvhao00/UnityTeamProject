using UnityEngine;
using UnityEngine.SceneManagement;

public class MovingBackground : MonoBehaviour
{
    private const string SceneName = "NewMainScene";
    private const string ResourceName = "BGLong3";
    private const string BackgroundObjectName = "Runtime Moving Background";
    private const int BackgroundSortingOrder = -1000;

    [SerializeField] private float zDistanceFromCamera = 10f;
    [SerializeField] private float scalePadding = 1.02f;
    [SerializeField] private float backgroundZoom = 1.5f;

    private Camera targetCamera;
    private SpriteRenderer spriteRenderer;
    private Vector2 levelRangeX;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        CreateForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode _)
    {
        CreateForScene(scene);
    }

    private static void CreateForScene(Scene scene)
    {
        if (!Application.isPlaying || scene.name != SceneName) return;
        if (GameObject.Find(BackgroundObjectName) != null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.orthographic) return;

        Sprite backgroundSprite = Resources.Load<Sprite>(ResourceName);
        if (backgroundSprite == null)
        {
            Texture2D backgroundTexture = Resources.Load<Texture2D>(ResourceName);
            if (backgroundTexture != null)
            {
                backgroundSprite = Sprite.Create(
                    backgroundTexture,
                    new Rect(0f, 0f, backgroundTexture.width, backgroundTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
        }

        if (backgroundSprite == null)
        {
            Debug.LogWarning($"Could not load Resources/{ResourceName} as a Sprite or Texture2D.");
            return;
        }

        GameObject backgroundObject = new(BackgroundObjectName);
        SceneManager.MoveGameObjectToScene(backgroundObject, scene);
        backgroundObject.transform.SetParent(mainCamera.transform, false);

        SpriteRenderer renderer = backgroundObject.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite;
        renderer.sortingOrder = BackgroundSortingOrder;

        MovingBackground movingBackground = backgroundObject.AddComponent<MovingBackground>();
        movingBackground.Initialize(mainCamera, renderer);
    }

    private void Initialize(Camera mainCamera, SpriteRenderer renderer)
    {
        targetCamera = mainCamera;
        spriteRenderer = renderer;
        levelRangeX = DetectLevelRangeX();
        FitToCameraHeight();
        UpdatePosition();
    }

    private void LateUpdate()
    {
        if (targetCamera == null || spriteRenderer == null) return;

        FitToCameraHeight();
        UpdatePosition();
    }

    private Vector2 DetectLevelRangeX()
    {
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;

        foreach (Collider2D collider in FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))
        {
            if (collider.CompareTag("Player")) continue;
            if (collider.transform.IsChildOf(transform)) continue;

            Bounds bounds = collider.bounds;
            minX = Mathf.Min(minX, bounds.min.x);
            maxX = Mathf.Max(maxX, bounds.max.x);
        }

        if (float.IsInfinity(minX) || float.IsInfinity(maxX) || float.IsNaN(minX) || float.IsNaN(maxX) || Mathf.Approximately(minX, maxX))
        {
            float cameraX = targetCamera != null ? targetCamera.transform.position.x : 0f;
            return new Vector2(cameraX, cameraX + 1f);
        }

        return new Vector2(minX, maxX);
    }

    private void FitToCameraHeight()
    {
        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        float scaleByHeight = viewHeight / spriteSize.y;
        float scaleByWidth = viewWidth / spriteSize.x;
        float scale = Mathf.Max(scaleByHeight, scaleByWidth) * backgroundZoom * scalePadding;

        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void UpdatePosition()
    {
        float viewWidth = targetCamera.orthographicSize * 2f * targetCamera.aspect;
        float backgroundWidth = spriteRenderer.sprite.bounds.size.x * transform.localScale.x;
        float extraWidth = Mathf.Max(0f, backgroundWidth - viewWidth);

        float progress = Mathf.InverseLerp(levelRangeX.x, levelRangeX.y, targetCamera.transform.position.x);
        float localX = Mathf.Lerp(extraWidth * 0.5f, -extraWidth * 0.5f, progress);

        transform.localPosition = new Vector3(localX, 0f, zDistanceFromCamera);
    }
}
