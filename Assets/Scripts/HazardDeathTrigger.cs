using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HazardDeathTrigger : MonoBehaviour
{
    [SerializeField] private bool killOnStay = true;

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        ConfigureCollider();
    }

    private void OnValidate()
    {
        ConfigureCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!killOnStay) return;

        TryKillPlayer(other);
    }

    private void ConfigureCollider()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void TryKillPlayer(Collider2D other)
    {
        PlayerDeath playerDeath = other.GetComponentInParent<PlayerDeath>();
        if (playerDeath == null || playerDeath.IsDead) return;

        playerDeath.Die(transform.position);
    }
}
