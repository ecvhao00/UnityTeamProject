using TarodevController;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerAbilityUnlock
{
    DoubleJump,
    WallJump
}

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class AbilityUnlockPickup : MonoBehaviour, IGameResettable
{
    [SerializeField] private PlayerAbilityUnlock ability;
    [SerializeField] private Vector2 size = new(0.65f, 0.65f);
    [SerializeField] private Color doubleJumpColor = new(0.2f, 1f, 0.35f, 1f);
    [SerializeField] private Color wallJumpColor = new(0.35f, 0.55f, 1f, 1f);

    private static Sprite generatedSquareSprite;
    private bool consumed;

    private void Awake()
    {
        EnsureSetup();
    }

    private void OnEnable()
    {
        EnsureSetup();
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
        EnsureSetup();
    }

    private void EnsureSetup()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer.sprite == null)
        {
            renderer.sprite = GetGeneratedSquareSprite();
        }

        renderer.color = ability == PlayerAbilityUnlock.DoubleJump ? doubleJumpColor : wallJumpColor;
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (!Application.isPlaying) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        consumed = true;

        switch (ability)
        {
            case PlayerAbilityUnlock.DoubleJump:
                player.UnlockDoubleJump();
                AbilityUnlockMessageDisplay.Show("You can double jump");
                break;
            case PlayerAbilityUnlock.WallJump:
                player.UnlockWallJump();
                AbilityUnlockMessageDisplay.Show("You can wall jump");
                break;
        }

        SetPickupVisible(false);
    }

    public void ResetForGameRestart()
    {
        consumed = false;
        SetPickupVisible(true);
    }

    private void SetPickupVisible(bool visible)
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        if (trigger != null)
        {
            trigger.enabled = visible;
        }

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private static Sprite GetGeneratedSquareSprite()
    {
        if (generatedSquareSprite != null) return generatedSquareSprite;

        Texture2D texture = new(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        generatedSquareSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
        generatedSquareSprite.hideFlags = HideFlags.HideAndDontSave;

        return generatedSquareSprite;
    }
}

public class AbilityUnlockMessageDisplay : MonoBehaviour
{
    private const float VisibleDuration = 1.25f;
    private const float FadeDuration = 1.75f;
    private const float TopMargin = 32f;
    private const float MessageHeight = 48f;

    private static AbilityUnlockMessageDisplay instance;

    private CanvasGroup canvasGroup;
    private Text messageText;
    private float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Prewarm()
    {
        EnsureInstance();
        instance.HideImmediate();
    }

    public static void Show(string message)
    {
        EnsureInstance();
        instance.ShowMessage(message);
    }

    public static void Hide()
    {
        if (instance == null) return;

        instance.HideImmediate();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        GameObject displayObject = new("Ability Unlock Message Display");
        instance = displayObject.AddComponent<AbilityUnlockMessageDisplay>();
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
    }

    private void ShowMessage(string message)
    {
        EnsureUi();
        messageText.text = message;
        canvasGroup.alpha = 1f;
        timer = 0f;
    }

    private void Update()
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0f) return;

        timer += Time.unscaledDeltaTime;

        if (timer <= VisibleDuration)
        {
            canvasGroup.alpha = 1f;
            return;
        }

        canvasGroup.alpha = Mathf.Clamp01(1f - (timer - VisibleDuration) / FadeDuration);
    }

    private void HideImmediate()
    {
        EnsureUi();
        canvasGroup.alpha = 0f;
        timer = VisibleDuration + FadeDuration;
    }

    private void EnsureUi()
    {
        if (canvasGroup != null && messageText != null) return;

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        Transform textTransform = transform.Find("Message");
        GameObject textObject = textTransform != null ? textTransform.gameObject : new GameObject("Message");
        textObject.transform.SetParent(transform, false);

        messageText = textObject.GetComponent<Text>();
        if (messageText == null)
        {
            messageText = textObject.AddComponent<Text>();
        }

        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 30;
        messageText.fontStyle = FontStyle.Bold;
        messageText.alignment = TextAnchor.UpperCenter;
        messageText.color = Color.white;
        messageText.raycastTarget = false;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -TopMargin);
        rectTransform.sizeDelta = new Vector2(0f, MessageHeight);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
