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
    [SerializeField] private Sprite electricOnSprite;
    [SerializeField] private Sprite electricOffSprite;
    [SerializeField] private SpriteDrawMode spriteDrawMode = SpriteDrawMode.Sliced;
    [SerializeField] private Color electricOnColor = Color.white;
    [SerializeField] private Color electricOffColor = Color.white;

    private BoxCollider2D triggerCollider;
    private SpriteRenderer spriteRenderer;
    private Coroutine cycleRoutine;
    private bool electricOn;

    private void Awake()
    {
        EnsureSetup();
        SetElectricState(startElectricOn, false);
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
            SetElectricState(startElectricOn, false);
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

        SetElectricState(startElectricOn, false);

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
            SetElectricState(startElectricOn, false);
        }
    }

    private void EnsureSetup()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = size;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = startElectricOn
                ? electricOnSprite != null ? electricOnSprite : RuntimeSpriteUtility.WhiteSquareSprite
                : electricOffSprite != null ? electricOffSprite : RuntimeSpriteUtility.WhiteSquareSprite;
        }

        ApplySpriteRendererSize();
        transform.localScale = Vector3.one;
    }

    private IEnumerator ElectricCycle()
    {
        if (startDelay > 0f)
        {
            SetElectricState(false, false);
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

    private void SetElectricState(bool active, bool playSfx = true)
    {
        bool wasElectricOn = electricOn;
        electricOn = active;

        if (spriteRenderer != null)
        {
            Sprite stateSprite = electricOn ? electricOnSprite : electricOffSprite;
            if (stateSprite != null)
            {
                spriteRenderer.sprite = stateSprite;
            }
            else if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = RuntimeSpriteUtility.WhiteSquareSprite;
            }

            ApplySpriteRendererSize();
            spriteRenderer.color = electricOn ? electricOnColor : electricOffColor;
        }

        if (playSfx && Application.isPlaying && electricOn && !wasElectricOn)
        {
            ElectricSfxManager.PlayAt(transform.position);
        }
    }

    private void ApplySpriteRendererSize()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.drawMode = spriteDrawMode;
        spriteRenderer.size = size;
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
            Mathf.Abs(size.x * transform.lossyScale.x),
            Mathf.Abs(size.y * transform.lossyScale.y)
        );

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(transform.position, worldSize, transform.eulerAngles.z);
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == triggerCollider) continue;
            TryKillPlayer(overlap);
        }
    }

}
