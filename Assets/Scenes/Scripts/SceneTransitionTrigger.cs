using TarodevController;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "EndingScene";
    [SerializeField] private Vector2 size = new(1f, 2f);
    [SerializeField] private Color editorColor = new(0.9f, 0.4f, 1f, 0.65f);
    [SerializeField] private bool hideRendererInPlay = true;

    private static Sprite generatedSquareSprite;

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

    private void Start()
    {
        if (Application.isPlaying && hideRendererInPlay)
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
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

        renderer.color = editorColor;
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isPlaying) return;
        if (SceneFader.IsTransitioning) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        SceneFader.LoadScene(targetSceneName);
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
