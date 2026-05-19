using System.Collections;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BrokenWireHazard : MonoBehaviour, IGameResettable
{
    [Header("Shape")]
    [SerializeField] private Vector2 size = new(2.5f, 0.35f);

    [Header("Timing")]
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private float electricOnDuration = 1f;
    [SerializeField] private float electricOffDuration = 1.25f;
    [SerializeField] private bool startElectricOn;

    [Header("Visual")]
    [SerializeField] private Color electricOnColor = new(0.2f, 0.85f, 1f, 0.9f);
    [SerializeField] private Color electricOffColor = new(0.25f, 0.25f, 0.25f, 0.45f);

    private BoxCollider2D triggerCollider;
    private SpriteRenderer spriteRenderer;
    private Coroutine cycleRoutine;
    private bool electricOn;

    private static Sprite generatedSquareSprite;

    private void Awake()
    {
        EnsureSetup();
        SetElectricState(startElectricOn);
    }

    private void OnEnable()
    {
        EnsureSetup();

        if (Application.isPlaying)
        {
            cycleRoutine = StartCoroutine(ElectricCycle());
        }
        else
        {
            SetElectricState(startElectricOn);
        }
    }

    private void OnDisable()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }
    }

    public void ResetForGameRestart()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }

        SetElectricState(startElectricOn);

        if (Application.isPlaying && isActiveAndEnabled)
        {
            cycleRoutine = StartCoroutine(ElectricCycle());
        }
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
        startDelay = Mathf.Max(0f, startDelay);
        electricOnDuration = Mathf.Max(0.05f, electricOnDuration);
        electricOffDuration = Mathf.Max(0.05f, electricOffDuration);

        EnsureSetup();

        if (!Application.isPlaying)
        {
            SetElectricState(startElectricOn);
        }
    }

    private void EnsureSetup()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = Vector2.one;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GetGeneratedSquareSprite();
        }

        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private IEnumerator ElectricCycle()
    {
        if (startDelay > 0f)
        {
            SetElectricState(false);
            yield return new WaitForSeconds(startDelay);
        }

        bool nextState = startElectricOn;

        while (true)
        {
            SetElectricState(nextState);

            if (electricOn)
            {
                CheckPlayersAlreadyInside();
                yield return new WaitForSeconds(electricOnDuration);
            }
            else
            {
                yield return new WaitForSeconds(electricOffDuration);
            }

            nextState = !nextState;
        }
    }

    private void SetElectricState(bool active)
    {
        electricOn = active;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = electricOn ? electricOnColor : electricOffColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void TryKillPlayer(Collider2D other)
    {
        if (!Application.isPlaying || !electricOn) return;

        PlayerDeath playerDeath = other.GetComponentInParent<PlayerDeath>();
        if (playerDeath == null) return;

        playerDeath.Die(transform.position);
    }

    private void CheckPlayersAlreadyInside()
    {
        Vector2 worldSize = new(
            Mathf.Abs(size.x * transform.lossyScale.x / transform.localScale.x),
            Mathf.Abs(size.y * transform.lossyScale.y / transform.localScale.y)
        );

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(transform.position, worldSize, transform.eulerAngles.z);
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == triggerCollider) continue;
            TryKillPlayer(overlap);
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
