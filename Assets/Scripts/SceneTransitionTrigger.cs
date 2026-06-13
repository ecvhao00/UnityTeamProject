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
            renderer.sprite = RuntimeSpriteUtility.WhiteSquareSprite;
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

}
