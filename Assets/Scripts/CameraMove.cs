using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float minCameraY = 0f;
    [SerializeField] private float cameraZ = -10f;

    private PlayerDeath playerDeath;

    public void SnapToPlayer()
    {
        if (player == null) return;

        transform.position = new Vector3(
            player.position.x,
            Mathf.Max(player.position.y, minCameraY),
            cameraZ
        );
    }

    private void LateUpdate()
    {
        if (player == null) return;
        if (playerDeath == null)
        {
            playerDeath = player.GetComponentInParent<PlayerDeath>();
        }

        if (playerDeath != null && playerDeath.IsDead) return;

        float targetY = Mathf.Max(player.position.y, minCameraY);

        Vector3 targetPosition = new Vector3(
            player.position.x,
            targetY,
            cameraZ
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
