using TarodevController;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SavePoint : MonoBehaviour
{
    [SerializeField] private Vector2 size = new(0.8f, 0.8f);
    [SerializeField] private Vector2 respawnOffset;
    [SerializeField] private Color inactiveColor = new(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color activeColor = new(0.2f, 1f, 0.8f, 1f);
    [SerializeField] private bool showMessage = true;

    private static Sprite generatedSquareSprite;
    private bool activated;

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
            renderer.sprite = GetGeneratedSquareSprite();
        }

        renderer.color = activated ? activeColor : inactiveColor;
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isPlaying) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        PlayerDeath playerDeath = player.GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            playerDeath = player.GetComponentInParent<PlayerDeath>();
        }

        if (playerDeath == null) return;

        Vector2 respawnPoint = (Vector2)transform.position + respawnOffset;
        playerDeath.SaveRespawnState(
            respawnPoint,
            player.DoubleJumpUnlocked,
            player.WallJumpUnlocked
        );

        activated = true;
        EnsureSetup();

        if (showMessage)
        {
            AbilityUnlockMessageDisplay.Show("Checkpoint saved");
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
