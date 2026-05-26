using UnityEngine;

public class FallingSignHitbox : MonoBehaviour
{
    private FallingSignTrap owner;

    public void Initialize(FallingSignTrap trap)
    {
        owner = trap;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        owner?.HandleSignCollision(collision);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        owner?.HandleSignTrigger(other);
    }
}
