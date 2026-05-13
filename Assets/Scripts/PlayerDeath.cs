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

    [Header("Knockback")]
    [SerializeField] private float knockbackX = 10f;
    [SerializeField] private float knockbackY = 12f;

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

    public bool IsDead => isDead;
    public Vector2 CurrentRespawnPoint => currentRespawnPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        currentRespawnPoint = initialRespawnPoint;

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
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
        currentRespawnPoint = respawnPoint;
    }

    public void Respawn()
    {
        isDead = false;

        if (rb != null)
        {
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
    }

    public void RestartGame()
    {
        ResetGameplayObjects();

        if (playerController != null)
        {
            playerController.ResetAbilityUnlocks();
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

    public void Die(Vector2 damageSourcePosition)
    {
        if (isDead) return;

        isDead = true;

        // 조작 비활성화
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // 반동 방향 계산
        float direction = transform.position.x >= damageSourcePosition.x ? 1f : -1f;

        // 기존 속도 제거 후 반동 적용
        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(direction * knockbackX, knockbackY);

        // 원하면 충돌 끄기
        // col.enabled = false;

        Debug.Log("Player Dead");
    }
}
