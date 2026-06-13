using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Spike : MonoBehaviour
{
    [SerializeField] private Vector2 size = new(1f, 0.75f);
    [SerializeField] private Color color = new(1f, 0.1f, 0.1f, 1f);

    private void Awake()
    {
        EnsureSetup();
    }

    private void OnEnable()
    {
        EnsureSetup();
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
        EnsureSetup();
    }

    private void EnsureSetup()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer.sprite == null)
        {
            renderer.sprite = RuntimeSpriteUtility.WhiteSquareSprite;
        }

        renderer.color = color;
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDeath playerDeath = other.GetComponent<PlayerDeath>();

        if (playerDeath == null) return;

        playerDeath.Die(transform.position);
    }

}
