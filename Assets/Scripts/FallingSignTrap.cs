using System.Collections;
using TarodevController;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class FallingSignTrap : MonoBehaviour, IGameResettable
{
    [Header("References")]
    [SerializeField] private Transform signBody;
    [SerializeField] private SpriteRenderer signRenderer;
    [SerializeField] private BoxCollider2D signCollider;
    [SerializeField] private Rigidbody2D signRigidbody;
    [SerializeField] private ParticleSystem dustWarning;

    [Header("Sign")]
    [SerializeField] private Vector2 signSize = new(2.5f, 0.75f);
    [SerializeField] private float signStartHeight = 4f;
    [SerializeField] private Color signColor = new(1f, 0.85f, 0.05f, 1f);
    [SerializeField] private bool signColliderAsTrigger;

    [Header("Timing")]
    [SerializeField] private float warningDuration = 0.6f;
    [SerializeField] private float resetDelay = 1.25f;
    [SerializeField] private bool resetAfterDrop = true;

    [Header("Warning")]
    [SerializeField] private bool useDustWarning = true;
    [SerializeField] private bool wobbleBeforeDrop;
    [SerializeField] private float wobblePositionAmplitude = 0.05f;
    [SerializeField] private float wobbleRotationAngle = 2f;
    [SerializeField] private float wobbleFrequency = 12f;

    [Header("Fall")]
    [SerializeField] private float fallGravityScale = 4f;

    private BoxCollider2D triggerCollider;
    private bool triggered;
    private bool falling;
    private Coroutine dropRoutine;

    private static Sprite generatedSquareSprite;

    private void Awake()
    {
        EnsureSetup();
        ResetSignToStart();
    }

    private void OnEnable()
    {
        EnsureSetup();

        if (!Application.isPlaying)
        {
            ResetSignToStart();
        }
    }

    private void OnValidate()
    {
        signSize.x = Mathf.Max(0.1f, signSize.x);
        signSize.y = Mathf.Max(0.1f, signSize.y);
        signStartHeight = Mathf.Max(0f, signStartHeight);
        warningDuration = Mathf.Max(0f, warningDuration);
        resetDelay = Mathf.Max(0f, resetDelay);
        fallGravityScale = Mathf.Max(0f, fallGravityScale);
        wobblePositionAmplitude = Mathf.Max(0f, wobblePositionAmplitude);
        wobbleRotationAngle = Mathf.Max(0f, wobbleRotationAngle);
        wobbleFrequency = Mathf.Max(0f, wobbleFrequency);

        EnsureSetup();

        if (!Application.isPlaying)
        {
            ResetSignToStart();
        }
    }

    private void EnsureSetup()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        if (signBody == null)
        {
            Transform existingSign = transform.Find("Sign");
            signBody = existingSign != null ? existingSign : new GameObject("Sign").transform;
            signBody.SetParent(transform, false);
        }

        if (signRenderer == null)
        {
            signRenderer = signBody.GetComponent<SpriteRenderer>();
            if (signRenderer == null)
            {
                signRenderer = signBody.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (signRenderer.sprite == null)
        {
            signRenderer.sprite = GetGeneratedSquareSprite();
        }

        signRenderer.color = signColor;

        if (signCollider == null)
        {
            signCollider = signBody.GetComponent<BoxCollider2D>();
            if (signCollider == null)
            {
                signCollider = signBody.gameObject.AddComponent<BoxCollider2D>();
            }
        }

        signCollider.isTrigger = signColliderAsTrigger;

        if (signRigidbody == null)
        {
            signRigidbody = signBody.GetComponent<Rigidbody2D>();
            if (signRigidbody == null)
            {
                signRigidbody = signBody.gameObject.AddComponent<Rigidbody2D>();
            }
        }

        signRigidbody.freezeRotation = true;

        FallingSignHitbox hitbox = signBody.GetComponent<FallingSignHitbox>();
        if (hitbox == null)
        {
            hitbox = signBody.gameObject.AddComponent<FallingSignHitbox>();
        }

        hitbox.Initialize(this);

        if (useDustWarning && dustWarning == null)
        {
            Transform existingDust = transform.Find("Dust Warning");
            dustWarning = existingDust != null ? existingDust.GetComponent<ParticleSystem>() : null;

            if (dustWarning == null)
            {
                GameObject dustObject = new("Dust Warning");
                dustObject.transform.SetParent(transform, false);
                dustWarning = dustObject.AddComponent<ParticleSystem>();
            }
        }

        ConfigureDust();
    }

    private void ConfigureDust()
    {
        if (!useDustWarning || dustWarning == null) return;

        dustWarning.transform.localPosition = new Vector3(0f, signStartHeight - signSize.y * 0.5f, 0f);

        ParticleSystem.MainModule main = dustWarning.main;
        main.loop = false;
        main.duration = Mathf.Max(0.05f, warningDuration);
        main.startLifetime = 0.45f;
        main.startSpeed = 1.2f;
        main.startSize = 0.08f;
        main.gravityModifier = 0.45f;
        main.startColor = new Color(0.8f, 0.65f, 0.32f, 0.9f);

        ParticleSystem.EmissionModule emission = dustWarning.emission;
        emission.rateOverTime = 24f;

        ParticleSystem.ShapeModule shape = dustWarning.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(signSize.x, 0.05f, 0f);

        if (!Application.isPlaying)
        {
            dustWarning.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isPlaying || triggered || !IsPlayer(other)) return;

        dropRoutine = StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        triggered = true;
        falling = false;

        if (useDustWarning && dustWarning != null)
        {
            dustWarning.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dustWarning.Play();
        }

        if (wobbleBeforeDrop && warningDuration > 0f)
        {
            yield return WobbleForSeconds(warningDuration);
        }
        else
        {
            yield return new WaitForSeconds(warningDuration);
        }

        ResetSignToStart();

        falling = true;
        signRigidbody.bodyType = RigidbodyType2D.Dynamic;
        signRigidbody.gravityScale = fallGravityScale;
        signRigidbody.linearVelocity = Vector2.zero;

        if (resetAfterDrop)
        {
            yield return new WaitForSeconds(resetDelay);
            ResetTrap();
        }
    }

    public void ResetForGameRestart()
    {
        ResetTrap();
    }

    private void ResetTrap()
    {
        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }

        triggered = false;
        falling = false;
        if (dustWarning != null)
        {
            dustWarning.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        ResetSignToStart();
    }

    private void ResetSignToStart()
    {
        if (signBody == null || signRigidbody == null) return;

        signRigidbody.linearVelocity = Vector2.zero;
        signRigidbody.angularVelocity = 0f;
        signRigidbody.bodyType = RigidbodyType2D.Kinematic;
        signRigidbody.gravityScale = 0f;

        signBody.localPosition = new Vector3(0f, signStartHeight, 0f);
        signBody.localRotation = Quaternion.identity;

        Vector2 spriteSize = GetSignSpriteSize();
        signBody.localScale = new Vector3(signSize.x / spriteSize.x, signSize.y / spriteSize.y, 1f);

        if (signCollider != null)
        {
            signCollider.isTrigger = signColliderAsTrigger;
            signCollider.size = spriteSize;
            signCollider.offset = Vector2.zero;
        }
    }

    private IEnumerator WobbleForSeconds(float duration)
    {
        float elapsed = 0f;
        Vector3 startPosition = new(0f, signStartHeight, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float wave = Mathf.Sin(elapsed * wobbleFrequency * Mathf.PI * 2f);
            Vector3 offset = Vector3.right * (wave * wobblePositionAmplitude);
            Quaternion rotation = Quaternion.Euler(0f, 0f, wave * wobbleRotationAngle);
            signBody.localPosition = startPosition + offset;
            signBody.localRotation = rotation;
            yield return null;
        }

        signBody.localPosition = startPosition;
        signBody.localRotation = Quaternion.identity;
    }

    private Vector2 GetSignSpriteSize()
    {
        if (signRenderer == null || signRenderer.sprite == null)
        {
            return Vector2.one;
        }

        Vector2 spriteSize = signRenderer.sprite.bounds.size;
        spriteSize.x = Mathf.Max(0.001f, spriteSize.x);
        spriteSize.y = Mathf.Max(0.001f, spriteSize.y);
        return spriteSize;
    }

    internal void HandleSignCollision(Collision2D collision)
    {
        if (!falling) return;

        PlayerDeath playerDeath = collision.collider.GetComponentInParent<PlayerDeath>();
        if (playerDeath == null) return;

        playerDeath.Die(signBody.position);
    }

    internal void HandleSignTrigger(Collider2D other)
    {
        if (!falling || !IsPlayer(other)) return;

        PlayerDeath playerDeath = other.GetComponentInParent<PlayerDeath>();
        if (playerDeath == null) return;

        playerDeath.Die(signBody.position);
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player")
            || other.GetComponentInParent<PlayerDeath>() != null
            || other.GetComponentInParent<PlayerController>() != null;
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
