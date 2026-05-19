using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider2D))]
public class ColapsePlatform : MonoBehaviour, IGameResettable
{
    [Header("Timing")]
    [FormerlySerializedAs("breakDelay")]
    [SerializeField] private float fallDelay = 0.5f;
    [SerializeField] private float respawnDelay = 2f;

    [Header("Fall")]
    [SerializeField] private Rigidbody2D platformRigidbody;
    [SerializeField] private float fallGravityScale = 4f;

    private Collider2D platformCollider;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isFalling;
    private Coroutine fallRoutine;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (platformRigidbody == null)
        {
            platformRigidbody = GetComponent<Rigidbody2D>();
        }

        if (platformRigidbody == null)
        {
            platformRigidbody = gameObject.AddComponent<Rigidbody2D>();
        }

        platformRigidbody.freezeRotation = true;
        ResetPlatformToStart();
    }

    private void OnValidate()
    {
        fallDelay = Mathf.Max(0f, fallDelay);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        fallGravityScale = Mathf.Max(0f, fallGravityScale);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFalling) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (IsPlayerOnTop(collision))
        {
            fallRoutine = StartCoroutine(FallRoutine());
        }
    }

    public void ResetForGameRestart()
    {
        ResetPlatform();
    }

    private void ResetPlatform()
    {
        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
            fallRoutine = null;
        }

        isFalling = false;
        ResetPlatformToStart();
    }

    private void ResetPlatformToStart()
    {
        if (platformCollider != null)
        {
            platformCollider.enabled = true;
        }

        if (platformRigidbody != null)
        {
            platformRigidbody.linearVelocity = Vector2.zero;
            platformRigidbody.angularVelocity = 0f;
            platformRigidbody.freezeRotation = true;
            platformRigidbody.bodyType = RigidbodyType2D.Kinematic;
            platformRigidbody.gravityScale = 0f;
        }

        transform.SetPositionAndRotation(startPosition, startRotation);
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

    private IEnumerator FallRoutine()
    {
        isFalling = true;

        yield return new WaitForSeconds(fallDelay);

        platformRigidbody.bodyType = RigidbodyType2D.Dynamic;
        platformRigidbody.gravityScale = fallGravityScale;
        platformRigidbody.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(respawnDelay);

        ResetPlatform();
    }
}
