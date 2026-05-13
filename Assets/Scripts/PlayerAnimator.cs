using UnityEngine;

namespace TarodevController
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] runSprites = new Sprite[5];
        [SerializeField, Min(1f)] private float runFramesPerSecond = 10f;
        [SerializeField] private bool faceRightByDefault = true;
        [SerializeField] private float moveThreshold = 0.01f;

        private IPlayerController player;
        private float runTimer;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            player = GetComponentInParent<IPlayerController>();
            ApplyIdleSprite();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (idleSprite == null && runSprites != null && runSprites.Length > 4)
            {
                idleSprite = runSprites[4];
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = GetComponentInParent<IPlayerController>();
            }

            float moveX = player != null ? player.FrameInput.x : Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(moveX) > moveThreshold)
            {
                spriteRenderer.flipX = faceRightByDefault ? moveX < 0f : moveX > 0f;
                AnimateRun();
                return;
            }

            runTimer = 0f;
            ApplyIdleSprite();
        }

        private void AnimateRun()
        {
            if (runSprites == null || runSprites.Length == 0)
            {
                ApplyIdleSprite();
                return;
            }

            runTimer += Time.deltaTime * runFramesPerSecond;
            int frameIndex = Mathf.FloorToInt(runTimer) % runSprites.Length;
            Sprite frame = runSprites[frameIndex];
            spriteRenderer.sprite = frame != null ? frame : idleSprite;
        }

        private void ApplyIdleSprite()
        {
            if (spriteRenderer != null && idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
        }
    }
}
