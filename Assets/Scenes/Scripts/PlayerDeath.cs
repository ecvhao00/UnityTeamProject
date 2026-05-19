using UnityEngine;
using TarodevController;

public interface IGameResettable
{
    void ResetForGameRestart();
}

public class PlayerDeath : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAnimator playerAnimator;

    [Header("Fall Death")]
    [SerializeField] private bool useFallDeath = true;
    [SerializeField] private float fallDeathY = -17f;

    [Header("Respawn")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private Vector2 initialRespawnPoint = new(0f, -6.3f);

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isDead;
    private Vector2 currentRespawnPoint;
    private bool savedDoubleJumpUnlocked;
    private bool savedWallJumpUnlocked;
    private float defaultGravityScale;
    private RigidbodyConstraints2D defaultConstraints;

    public bool IsDead => isDead;
    public Vector2 CurrentRespawnPoint => currentRespawnPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        currentRespawnPoint = initialRespawnPoint;

        if (rb != null)
        {
            defaultGravityScale = rb.gravityScale;
            defaultConstraints = rb.constraints | RigidbodyConstraints2D.FreezeRotation;
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerController != null)
        {
            savedDoubleJumpUnlocked = playerController.DoubleJumpUnlocked;
            savedWallJumpUnlocked = playerController.WallJumpUnlocked;
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<PlayerAnimator>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
            return;
        }

        if (!useFallDeath || isDead) return;
        if (transform.position.y > fallDeathY) return;

        Die(transform.position);
    }

    public void SetRespawnPoint(Vector2 respawnPoint)
    {
        SaveRespawnState(
            respawnPoint,
            playerController != null && playerController.DoubleJumpUnlocked,
            playerController != null && playerController.WallJumpUnlocked
        );
    }

    public void SaveRespawnState(Vector2 respawnPoint, bool doubleJumpUnlocked, bool wallJumpUnlocked)
    {
        currentRespawnPoint = respawnPoint;
        savedDoubleJumpUnlocked = doubleJumpUnlocked;
        savedWallJumpUnlocked = wallJumpUnlocked;
    }

    public void Respawn()
    {
        isDead = false;

        if (rb != null)
        {
            rb.gravityScale = defaultGravityScale;
            rb.constraints = defaultConstraints;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        transform.position = currentRespawnPoint;
        Physics2D.SyncTransforms();

        if (col != null)
        {
            col.enabled = true;
        }

        if (playerController != null)
        {
            playerController.ResetMovementState();
            playerController.enabled = true;
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetDead(false);
        }
    }

    public void RestartGame()
    {
        ResetGameplayObjects();

        if (playerController != null)
        {
            playerController.SetAbilityUnlocks(savedDoubleJumpUnlocked, savedWallJumpUnlocked);
        }

        Respawn();

        foreach (CameraMove cameraMove in FindObjectsByType<CameraMove>(FindObjectsInactive.Exclude))
        {
            cameraMove.SnapToPlayer();
        }

        AbilityUnlockMessageDisplay.Hide();
    }

    private static void ResetGameplayObjects()
    {
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (behaviour is IGameResettable resettable)
            {
                resettable.ResetForGameRestart();
            }
        }
    }

    public void Die(Vector2 _)
    {
        if (isDead) return;

        isDead = true;

        if (playerController != null)
        {
            playerController.ResetMovementState();
            playerController.enabled = false;
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetDead(true);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        Debug.Log("Player Dead");
    }
}
