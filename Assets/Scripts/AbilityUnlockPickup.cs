using System.Collections;
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
            renderer.sprite = RuntimeSpriteUtility.WhiteSquareSprite;
        }

        renderer.color = usesGeneratedSquare || renderer.sprite == RuntimeSpriteUtility.WhiteSquareSprite
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
        MainSceneBgm.PlayCollect();

        switch (ability)
        {
            case PlayerAbilityUnlock.DoubleJump:
                player.UnlockDoubleJump();
                AbilityUnlockWorldMessageDisplay.Show("You can double jump", player.transform);
                break;
            case PlayerAbilityUnlock.WallJump:
                player.UnlockWallJump();
                AbilityUnlockWorldMessageDisplay.Show("You can wall jump", player.transform);
                break;
        }

        if (SaveRespawnState(player))
        {
            AbilityUnlockMessageDisplay.Show("Checkpoint saved");
        }

        SetPickupVisible(false);
    }

    private bool SaveRespawnState(PlayerController player)
    {
        if (!saveRespawnPoint) return false;

        PlayerDeath playerDeath = player.GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            playerDeath = player.GetComponentInParent<PlayerDeath>();
        }

        if (playerDeath == null) return false;

        Vector2 respawnPoint = (Vector2)GetBaseWorldPosition() + respawnOffset;
        playerDeath.SaveRespawnState(
            respawnPoint,
            player.DoubleJumpUnlocked,
            player.WallJumpUnlocked
        );
        return true;
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
    private Coroutine messageRoutine;

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
        AbilityUnlockWorldMessageDisplay.Hide();
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

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(ShowRoutine());
    }

    private void HideImmediate()
    {
        EnsureUi();
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        canvasGroup.alpha = 0f;
        SetMessagePosition(GetOffscreenPosition());
    }

    private void EnsureUi()
    {
        if (canvasGroup != null && messageText != null && messageRect != null) return;

        RuntimeUiUtility.SetupOverlayCanvas(gameObject, 100);
        canvasGroup = RuntimeUiUtility.SetupCanvasGroup(gameObject, false);

        messageText = RuntimeUiUtility.CreateText(transform, "Message", "", FontSize, FontStyle.Bold, TextAnchor.UpperCenter);
        Outline outline = messageText.gameObject.GetOrAdd<Outline>();

        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        messageRect = messageText.rectTransform;
        messageRect.anchorMin = new Vector2(0f, 1f);
        messageRect.anchorMax = new Vector2(1f, 1f);
        messageRect.pivot = new Vector2(0.5f, 1f);
        messageRect.anchoredPosition = GetOffscreenPosition();
        messageRect.sizeDelta = new Vector2(0f, MessageHeight);
    }

    private IEnumerator ShowRoutine()
    {
        canvasGroup.alpha = 1f;

        yield return Slide(GetOffscreenPosition(), GetTargetPosition());
        yield return new WaitForSecondsRealtime(VisibleDuration);
        yield return Slide(GetTargetPosition(), GetOffscreenPosition());

        canvasGroup.alpha = 0f;
        messageRoutine = null;
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SlideDuration));
            SetMessagePosition(Vector2.LerpUnclamped(from, to, progress));
            yield return null;
        }

        SetMessagePosition(to);
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

public class AbilityUnlockWorldMessageDisplay : MonoBehaviour
{
    private const float VisibleDuration = 1.5f;
    private const float FadeDuration = 0.75f;
    private const float WorldOffsetY = 1.5f;
    private const float MessageHeight = 64f;
    private const int FontSize = 30;

    private static AbilityUnlockWorldMessageDisplay instance;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform canvasRect;
    private RectTransform messageRect;
    private Text messageText;
    private Coroutine messageRoutine;
    private Transform followTarget;

    public static void Show(string message, Transform target)
    {
        EnsureInstance();
        instance.ShowMessage(message, target);
    }

    public static void Hide()
    {
        if (instance == null) return;

        instance.HideImmediate();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        GameObject displayObject = new("Ability Unlock World Message Display");
        instance = displayObject.AddComponent<AbilityUnlockWorldMessageDisplay>();
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
        HideImmediate();
    }

    private void ShowMessage(string message, Transform target)
    {
        EnsureUi();
        followTarget = target;
        messageText.text = message;

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        canvasGroup.alpha = 1f;

        yield return FollowForSeconds(VisibleDuration, 1f);

        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeDuration);
            UpdatePosition();
            yield return null;
        }

        HideImmediate();
    }

    private IEnumerator FollowForSeconds(float duration, float alpha)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = alpha;
            UpdatePosition();
            yield return null;
        }
    }

    private void HideImmediate()
    {
        EnsureUi();
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        followTarget = null;
        canvasGroup.alpha = 0f;
    }

    private void EnsureUi()
    {
        if (canvasGroup != null && messageText != null && messageRect != null) return;

        canvas = RuntimeUiUtility.SetupOverlayCanvas(gameObject, 101);
        canvasRect = canvas.GetComponent<RectTransform>();
        canvasGroup = RuntimeUiUtility.SetupCanvasGroup(gameObject, false);

        messageText = RuntimeUiUtility.CreateText(transform, "Message", "", FontSize, FontStyle.Bold);
        messageText.alignment = TextAnchor.MiddleCenter;
        Outline outline = messageText.gameObject.GetOrAdd<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        messageRect = messageText.rectTransform;
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.sizeDelta = new Vector2(620f, MessageHeight);
    }

    private void UpdatePosition()
    {
        if (followTarget == null || Camera.main == null) return;

        Vector3 worldPosition = followTarget.position + Vector3.up * WorldOffsetY;
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPosition))
        {
            messageRect.anchoredPosition = localPosition;
        }
    }
}
