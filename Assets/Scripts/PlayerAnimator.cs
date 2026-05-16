using UnityEngine;

namespace TarodevController
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] runSprites = new Sprite[5];
        [SerializeField] private Sprite[] jumpSprites = new Sprite[6];
        [SerializeField] private Sprite damagedSprite;
        [SerializeField, Min(1f)] private float runFramesPerSecond = 10f;
        [SerializeField, Min(1f)] private float jumpFramesPerSecond = 15f;
        [SerializeField] private float[] jumpRotationAngles = { 34f, 22f, 10f, 0f, -12f, -24f };
        [SerializeField, Min(0)] private int wallClingRunSpriteIndex = 2;
        [SerializeField] private float wallClingRightWallAngle = 90f;
        [SerializeField] private float wallClingLeftWallAngle = 270f;
        [SerializeField] private bool faceRightByDefault = true;
        [SerializeField] private float moveThreshold = 0.01f;

        private const int FallStartJumpFrame = 4;

        private IPlayerController player;
        private float runTimer;
        private float jumpTimer;
        private bool isGrounded = true;
        private bool isDead;
        private bool airborneFromJump;
        private Quaternion defaultLocalRotation;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            defaultLocalRotation = transform.localRotation;
            SetPlayer(GetComponentInParent<IPlayerController>());
            ApplyIdleSprite();
        }

        private void OnDestroy()
        {
            ResetVisualRotation();
            SetPlayer(null);
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
                SetPlayer(GetComponentInParent<IPlayerController>());
            }

            if (isDead)
            {
                ApplyDamagedSprite();
                return;
            }

            float moveX = player != null ? player.FrameInput.x : Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(moveX) > moveThreshold)
            {
                spriteRenderer.flipX = faceRightByDefault ? moveX < 0f : moveX > 0f;
            }

            if (IsWallClinging())
            {
                runTimer = 0f;
                jumpTimer = 0f;
                airborneFromJump = false;
                ApplyWallClingSprite();
                return;
            }

            if (!isGrounded && HasAnySprite(jumpSprites))
            {
                runTimer = 0f;
                AnimateJump();
                return;
            }

            if (Mathf.Abs(moveX) > moveThreshold)
            {
                jumpTimer = 0f;
                ResetVisualRotation();
                AnimateRun();
                return;
            }

            runTimer = 0f;
            jumpTimer = 0f;
            ResetVisualRotation();
            ApplyIdleSprite();
        }

        public void SetDead(bool dead)
        {
            if (isDead == dead) return;

            isDead = dead;
            runTimer = 0f;
            jumpTimer = 0f;
            airborneFromJump = false;
            ResetVisualRotation();

            if (isDead)
            {
                ApplyDamagedSprite();
            }
            else
            {
                ApplyIdleSprite();
            }
        }

        private void SetPlayer(IPlayerController nextPlayer)
        {
            if (ReferenceEquals(player, nextPlayer)) return;

            if (player != null)
            {
                player.GroundedChanged -= OnGroundedChanged;
                player.Jumped -= OnJumped;
            }

            player = nextPlayer;

            if (player != null)
            {
                player.GroundedChanged += OnGroundedChanged;
                player.Jumped += OnJumped;
            }
        }

        private void OnGroundedChanged(bool grounded, float _)
        {
            isGrounded = grounded;

            if (grounded)
            {
                jumpTimer = 0f;
                airborneFromJump = false;
                ResetVisualRotation();
            }
        }

        private void OnJumped()
        {
            isGrounded = false;
            jumpTimer = 0f;
            airborneFromJump = true;
        }

        private bool IsWallClinging()
        {
            return player != null
                && !isGrounded
                && player.WallJumpUnlocked
                && player.IsTouchingWall
                && player.WallDirection != 0;
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

        private void AnimateJump()
        {
            jumpTimer += Time.deltaTime * jumpFramesPerSecond;
            int startFrame = airborneFromJump ? 0 : Mathf.Min(FallStartJumpFrame, jumpSprites.Length - 1);
            int frameIndex = Mathf.Min(startFrame + Mathf.FloorToInt(jumpTimer), jumpSprites.Length - 1);
            Sprite frame = jumpSprites[frameIndex];
            spriteRenderer.sprite = frame != null ? frame : idleSprite;
            ApplyJumpRotation(frameIndex);
        }

        private void ApplyIdleSprite()
        {
            if (spriteRenderer != null && idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
        }

        private void ApplyWallClingSprite()
        {
            if (spriteRenderer == null) return;

            int wallDirection = player != null ? player.WallDirection : 0;

            if (wallDirection != 0)
            {
                spriteRenderer.flipX = faceRightByDefault ? wallDirection < 0 : wallDirection > 0;
            }

            Sprite frame = GetRunSprite(wallClingRunSpriteIndex);
            spriteRenderer.sprite = frame != null ? frame : idleSprite;

            float angle = wallDirection > 0 ? wallClingRightWallAngle : wallClingLeftWallAngle;
            transform.localRotation = defaultLocalRotation * Quaternion.Euler(0f, 0f, angle);
        }

        private void ApplyDamagedSprite()
        {
            ResetVisualRotation();

            if (spriteRenderer != null && damagedSprite != null)
            {
                spriteRenderer.sprite = damagedSprite;
            }
        }

        private void ApplyJumpRotation(int frameIndex)
        {
            float angle = GetJumpRotationAngle(frameIndex);

            if (spriteRenderer != null && spriteRenderer.flipX)
            {
                angle = -angle;
            }

            transform.localRotation = defaultLocalRotation * Quaternion.Euler(0f, 0f, angle);
        }

        private float GetJumpRotationAngle(int frameIndex)
        {
            if (jumpRotationAngles == null || jumpRotationAngles.Length == 0) return 0f;

            int angleIndex = Mathf.Clamp(frameIndex, 0, jumpRotationAngles.Length - 1);
            return jumpRotationAngles[angleIndex];
        }

        private void ResetVisualRotation()
        {
            transform.localRotation = defaultLocalRotation;
        }

        private Sprite GetRunSprite(int index)
        {
            if (runSprites == null || runSprites.Length == 0) return null;

            int frameIndex = Mathf.Clamp(index, 0, runSprites.Length - 1);
            return runSprites[frameIndex];
        }

        private static bool HasAnySprite(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0) return false;

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null) return true;
            }

            return false;
        }
    }
}
