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
    [Header("Ability")]
    [SerializeField] private PlayerAbilityUnlock ability;

    [Header("Save Point")]
    [SerializeField] private bool saveRespawnPoint = true;
    [SerializeField] private Vector2 respawnOffset;
    [SerializeField] private bool reappearOnRestart;

    [Header("Visual")]
    [SerializeField] private Vector2 size = new(0.65f, 0.65f);
    [SerializeField] private Color doubleJumpColor = new(0.2f, 1f, 0.35f, 1f);
    [SerializeField] private Color wallJumpColor = new(0.35f, 0.55f, 1f, 1f);

    [Header("Float Motion")]
    [SerializeField] private bool floatMotion = true;
    [SerializeField] private float floatAmplitude = 0.06f;
    [SerializeField] private float floatFrequency = 1.2f;
    [SerializeField] private float floatPhaseOffset;

    private static Sprite generatedSquareSprite;
    private bool consumed;
    private Vector3 baseLocalPosition;
    private bool hasBaseLocalPosition;

    private void Awake()
    {
        CaptureFloatOrigin();
        EnsureSetup();
    }

    private void OnEnable()
    {
        CaptureFloatOrigin();
        EnsureSetup();
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
        floatAmplitude = Mathf.Max(0f, floatAmplitude);
        floatFrequency = Mathf.Max(0f, floatFrequency);
        EnsureSetup();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        UpdateFloatMotion();
    }

    private void EnsureSetup()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        bool usesGeneratedSquare = renderer.sprite == null;
        if (renderer.sprite == null)
        {
            renderer.sprite = GetGeneratedSquareSprite();
        }

        renderer.color = usesGeneratedSquare || renderer.sprite == generatedSquareSprite
            ? GetAbilityColor()
            : Color.white;
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private Color GetAbilityColor()
    {
        return ability == PlayerAbilityUnlock.DoubleJump ? doubleJumpColor : wallJumpColor;
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

        SaveRespawnState(player);
        SetPickupVisible(false);
    }

    private void SaveRespawnState(PlayerController player)
    {
        if (!saveRespawnPoint) return;

        PlayerDeath playerDeath = player.GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            playerDeath = player.GetComponentInParent<PlayerDeath>();
        }

        if (playerDeath == null) return;

        Vector2 respawnPoint = (Vector2)GetBaseWorldPosition() + respawnOffset;
        playerDeath.SaveRespawnState(
            respawnPoint,
            player.DoubleJumpUnlocked,
            player.WallJumpUnlocked
        );
    }

    public void ResetForGameRestart()
    {
        if (reappearOnRestart)
        {
            consumed = false;
            SetPickupVisible(true);
            return;
        }

        if (!consumed && IsAbilityAlreadyUnlocked())
        {
            consumed = true;
        }

        SetPickupVisible(!consumed);
    }

    private bool IsAbilityAlreadyUnlocked()
    {
        if (!Application.isPlaying) return false;

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) return false;

        return ability switch
        {
            PlayerAbilityUnlock.DoubleJump => player.DoubleJumpUnlocked,
            PlayerAbilityUnlock.WallJump => player.WallJumpUnlocked,
            _ => false
        };
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

    private void CaptureFloatOrigin()
    {
        if (hasBaseLocalPosition) return;

        baseLocalPosition = transform.localPosition;
        hasBaseLocalPosition = true;
    }

    private void UpdateFloatMotion()
    {
        CaptureFloatOrigin();

        if (!floatMotion || Mathf.Approximately(floatAmplitude, 0f) || Mathf.Approximately(floatFrequency, 0f))
        {
            transform.localPosition = baseLocalPosition;
            return;
        }

        float offsetY = Mathf.Sin((Time.time * floatFrequency + floatPhaseOffset) * Mathf.PI * 2f) * floatAmplitude;
        transform.localPosition = baseLocalPosition + new Vector3(0f, offsetY, 0f);
    }

    private Vector3 GetBaseWorldPosition()
    {
        CaptureFloatOrigin();
        return transform.parent == null ? baseLocalPosition : transform.parent.TransformPoint(baseLocalPosition);
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
    private const float SlideDuration = 0.45f;
    private const float VisibleDuration = 1.25f;
    private const float TopMargin = 32f;
    private const float MessageHeight = 72f;
    private const float OffscreenPadding = 24f;
    private const int FontSize = 44;

    private static AbilityUnlockMessageDisplay instance;

    private CanvasGroup canvasGroup;
    private Text messageText;
    private RectTransform messageRect;
    private float timer;
    private MessageState state = MessageState.Hidden;

    private enum MessageState
    {
        Hidden,
        Entering,
        Visible,
        Exiting
    }

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
        state = MessageState.Entering;
        SetMessagePosition(GetOffscreenPosition());
    }

    private void Update()
    {
        if (canvasGroup == null || messageRect == null || state == MessageState.Hidden) return;

        timer += Time.unscaledDeltaTime;

        if (state == MessageState.Entering)
        {
            float progress = Mathf.Clamp01(timer / SlideDuration);
            SetMessageProgress(GetOffscreenPosition(), GetTargetPosition(), progress);

            if (progress >= 1f)
            {
                timer = 0f;
                state = MessageState.Visible;
            }

            return;
        }

        if (state == MessageState.Visible)
        {
            SetMessagePosition(GetTargetPosition());

            if (timer >= VisibleDuration)
            {
                timer = 0f;
                state = MessageState.Exiting;
            }

            return;
        }

        float exitProgress = Mathf.Clamp01(timer / SlideDuration);
        SetMessageProgress(GetTargetPosition(), GetOffscreenPosition(), exitProgress);

        if (exitProgress >= 1f)
        {
            HideImmediate();
        }
    }

    private void HideImmediate()
    {
        EnsureUi();
        canvasGroup.alpha = 0f;
        timer = 0f;
        state = MessageState.Hidden;
        SetMessagePosition(GetOffscreenPosition());
    }

    private void EnsureUi()
    {
        if (canvasGroup != null && messageText != null && messageRect != null) return;

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
        messageText.fontSize = FontSize;
        messageText.fontStyle = FontStyle.Bold;
        messageText.alignment = TextAnchor.UpperCenter;
        messageText.color = Color.white;
        messageText.raycastTarget = false;
        Outline outline = textObject.GetComponent<Outline>();
        
        if (outline == null)
        {
        outline = textObject.AddComponent<Outline>();
        }

        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        messageRect = textObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0f, 1f);
        messageRect.anchorMax = new Vector2(1f, 1f);
        messageRect.pivot = new Vector2(0.5f, 1f);
        messageRect.anchoredPosition = state == MessageState.Hidden ? GetOffscreenPosition() : GetTargetPosition();
        messageRect.sizeDelta = new Vector2(0f, MessageHeight);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void SetMessageProgress(Vector2 from, Vector2 to, float progress)
    {
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
        SetMessagePosition(Vector2.LerpUnclamped(from, to, easedProgress));
    }

    private void SetMessagePosition(Vector2 position)
    {
        if (messageRect != null)
        {
            messageRect.anchoredPosition = position;
        }
    }

    private static Vector2 GetTargetPosition()
    {
        return new Vector2(0f, -TopMargin);
    }

    private static Vector2 GetOffscreenPosition()
    {
        return new Vector2(0f, MessageHeight + OffscreenPadding);
    }
}
