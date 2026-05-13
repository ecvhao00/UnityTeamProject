using System.Collections;
using UnityEngine;

public class ColapsePlatform : MonoBehaviour, IGameResettable
{
    [Header("Timing")]
    [SerializeField] private float breakDelay = 0.5f;
    [SerializeField] private float respawnDelay = 2f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Collider2D platformCollider;
    private bool isBreaking;
    private Coroutine breakRoutine;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBreaking) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (IsPlayerOnTop(collision))
        {
            breakRoutine = StartCoroutine(BreakRoutine());
        }
    }

    public void ResetForGameRestart()
    {
        if (breakRoutine != null)
        {
            StopCoroutine(breakRoutine);
            breakRoutine = null;
        }

        isBreaking = false;

        if (platformCollider != null)
        {
            platformCollider.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private bool IsPlayerOnTop(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator BreakRoutine()
    {
        isBreaking = true;

        yield return new WaitForSeconds(breakDelay);

        platformCollider.enabled = false;
        spriteRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        platformCollider.enabled = true;
        spriteRenderer.enabled = true;

        isBreaking = false;
        breakRoutine = null;
    }
}
