using UnityEngine;

namespace TarodevController
{
    [CreateAssetMenu]
    public class ScriptableStats : ScriptableObject
    {
        [Header("LAYERS")]
        [Tooltip("Set this to the layer your player is on")]
        public LayerMask PlayerLayer;

        [Header("INPUT")]
        [Tooltip("Makes all Input snap to an integer. Prevents gamepads from walking slowly.")]
        public bool SnapInput = true;

        [Tooltip("Minimum input required before you mount a ladder or climb a ledge."), Range(0.01f, 0.99f)]
        public float VerticalDeadZoneThreshold = 0.3f;

        [Tooltip("Minimum input required before a left or right is recognized."), Range(0.01f, 0.99f)]
        public float HorizontalDeadZoneThreshold = 0.1f;

        [Header("MOVEMENT")]
        [Tooltip("The top horizontal movement speed")]
        public float MaxSpeed = 14;

        [Tooltip("The player's capacity to gain horizontal speed")]
        public float Acceleration = 120;

        [Tooltip("The pace at which the player comes to a stop")]
        public float GroundDeceleration = 60;

        [Tooltip("Deceleration in air only after stopping input mid-air")]
        public float AirDeceleration = 30;

        [Tooltip("A constant downward force applied while grounded. Helps on slopes"), Range(0f, -10f)]
        public float GroundingForce = -1.5f;

        [Tooltip("The detection distance for grounding and roof detection"), Range(0f, 0.5f)]
        public float GrounderDistance = 0.05f;

        [Header("JUMP")]
        [Tooltip("The immediate velocity applied when jumping")]
        public float JumpPower = 36;

        [Tooltip("The maximum vertical movement speed")]
        public float MaxFallSpeed = 40;

        [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
        public float FallAcceleration = 110;

        [Tooltip("The gravity multiplier added when jump is released early")]
        public float JumpEndEarlyGravityModifier = 3;

        [Tooltip("The time before coyote jump becomes unusable.")]
        public float CoyoteTime = .15f;

        [Tooltip("The amount of time we buffer a jump.")]
        public float JumpBuffer = .2f;

        [Header("DOUBLE JUMP")]
        [Tooltip("공중에서 추가로 점프할 수 있는 횟수. 1이면 2단 점프")]
        public int ExtraJumps = 1;

        [Tooltip("공중 추가 점프의 힘")]
        public float AirJumpPower = 32f;

        [Header("WALL JUMP")]
        [Tooltip("벽 감지 거리")]
        [Range(0f, 0.5f)]
        public float WallCheckDistance = 0.08f;

        [Tooltip("벽 점프 시 가로 방향 힘")]
        public float WallJumpPowerX = 24f;

        [Tooltip("벽 점프 시 세로 방향 힘")]
        public float WallJumpPowerY = 34f;

        [Tooltip("벽 점프 직후 플레이어 입력을 잠깐 무시하는 시간")]
        public float WallJumpInputLockTime = 0.18f;

        [Tooltip("벽에 붙어 있을 때 최대 낙하 속도")]
        public float WallSlideMaxFallSpeed = 8f;
    }
}