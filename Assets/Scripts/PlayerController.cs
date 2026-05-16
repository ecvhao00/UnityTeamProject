using System;
using UnityEngine;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;

        [Header("Ability Unlocks")]
        [SerializeField] private bool doubleJumpUnlocked;
        [SerializeField] private bool wallJumpUnlocked;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;
        private readonly Collider2D[] _collisionResults = new Collider2D[8];

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public bool DoubleJumpUnlocked => doubleJumpUnlocked;
        public bool WallJumpUnlocked => wallJumpUnlocked;
        public bool IsTouchingWall => _touchingWall;
        public int WallDirection => _wallDirection;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

        public void UnlockDoubleJump()
        {
            doubleJumpUnlocked = true;
            ResetAirJumps();
        }

        public void UnlockWallJump()
        {
            wallJumpUnlocked = true;
        }

        public void ResetAbilityUnlocks()
        {
            doubleJumpUnlocked = false;
            wallJumpUnlocked = false;
            ResetMovementState();
        }

        public void SetAbilityUnlocks(bool doubleJump, bool wallJump)
        {
            doubleJumpUnlocked = doubleJump;
            wallJumpUnlocked = wallJump;
            ResetAirJumps();
        }

        public void ResetMovementState()
        {
            _frameInput = default;
            _frameVelocity = Vector2.zero;
            _jumpToConsume = false;
            _bufferedJumpUsable = false;
            _endedJumpEarly = false;
            _coyoteUsable = false;
            _timeJumpWasPressed = 0f;
            _frameLeftGrounded = float.MinValue;
            _grounded = false;
            _touchingWall = false;
            _wallDirection = 0;
            _wallJumpLockUntil = 0f;

            ResetAirJumps();
        }

        private float _time;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            _rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold
                    ? 0
                    : Mathf.Sign(_frameInput.Move.x);

                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold
                    ? 0
                    : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            CheckCollisions();

            HandleJump();
            HandleDirection();
            HandleGravity();

            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private bool _touchingWall;
        private int _wallDirection;

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            Bounds bounds = _col.bounds;
            float verticalCheckHeight = Mathf.Max(_stats.GrounderDistance, 0.03f);
            float wallCheckWidth = Mathf.Max(_stats.WallCheckDistance, 0.03f);

            Vector2 verticalCheckSize = new(
                Mathf.Max(bounds.size.x * 0.9f, 0.05f),
                verticalCheckHeight
            );

            Vector2 horizontalCheckSize = new(
                wallCheckWidth,
                Mathf.Max(bounds.size.y * 0.85f, 0.05f)
            );

            bool groundHit = HasSolidOverlap(
                new Vector2(bounds.center.x, bounds.min.y - verticalCheckSize.y * 0.5f),
                verticalCheckSize
            );

            bool ceilingHit = HasSolidOverlap(
                new Vector2(bounds.center.x, bounds.max.y + verticalCheckSize.y * 0.5f),
                verticalCheckSize
            );

            bool wallLeftHit = HasSolidOverlap(
                new Vector2(bounds.min.x - horizontalCheckSize.x * 0.5f, bounds.center.y),
                horizontalCheckSize
            );

            bool wallRightHit = HasSolidOverlap(
                new Vector2(bounds.max.x + horizontalCheckSize.x * 0.5f, bounds.center.y),
                horizontalCheckSize
            );

            _touchingWall = !_grounded && (wallLeftHit || wallRightHit);

            if (wallLeftHit)
            {
                _wallDirection = -1;
            }
            else if (wallRightHit)
            {
                _wallDirection = 1;
            }
            else
            {
                _wallDirection = 0;
            }

            if (ceilingHit)
            {
                _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);
            }

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;

                ResetAirJumps();

                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        private bool HasSolidOverlap(Vector2 center, Vector2 size)
        {
            ContactFilter2D filter = new()
            {
                useLayerMask = true,
                layerMask = ~_stats.PlayerLayer,
                useTriggers = false
            };

            int hitCount = Physics2D.OverlapBox(center, size, 0f, filter, _collisionResults);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _collisionResults[i];
                if (hit == null || hit == _col) continue;
                if (hit.attachedRigidbody == _rb) continue;

                return true;
            }

            return false;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private int _airJumpsRemaining;
        private float _wallJumpLockUntil;

        private bool HasBufferedJump =>
            _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;

        private bool CanUseCoyote =>
            _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private bool CanWallJump =>
            wallJumpUnlocked && !_grounded && _touchingWall && _wallDirection != 0;

        private bool IsWallJumpInputLocked =>
            _time < _wallJumpLockUntil;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0)
            {
                _endedJumpEarly = true;
            }

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (CanWallJump)
            {
                ExecuteWallJump();
            }
            else if (_grounded || CanUseCoyote)
            {
                ExecuteJump(_stats.JumpPower);
            }
            else if (_airJumpsRemaining > 0)
            {
                ExecuteAirJump();
            }

            _jumpToConsume = false;
        }

        private void ExecuteJump(float jumpPower)
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity.y = jumpPower;

            Jumped?.Invoke();
        }

        private void ExecuteAirJump()
        {
            _airJumpsRemaining--;

            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity.y = _stats.AirJumpPower;

            Jumped?.Invoke();
        }

        private void ExecuteWallJump()
        {
            int jumpDirection = -_wallDirection;

            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity.x = jumpDirection * _stats.WallJumpPowerX;
            _frameVelocity.y = _stats.WallJumpPowerY;

            _wallJumpLockUntil = _time + _stats.WallJumpInputLockTime;

            ResetAirJumps();

            Jumped?.Invoke();
        }

        private void ResetAirJumps()
        {
            _airJumpsRemaining = doubleJumpUnlocked ? _stats.ExtraJumps : 0;
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (IsWallJumpInputLocked)
            {
                return;
            }

            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    0,
                    deceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    _frameInput.Move.x * _stats.MaxSpeed,
                    _stats.Acceleration * Time.fixedDeltaTime
                );
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var maxFallSpeed = _stats.MaxFallSpeed;

                if (wallJumpUnlocked && _touchingWall && _frameVelocity.y < 0)
                {
                    maxFallSpeed = _stats.WallSlideMaxFallSpeed;
                }

                var inAirGravity = _stats.FallAcceleration;

                if (_endedJumpEarly && _frameVelocity.y > 0)
                {
                    inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                }

                _frameVelocity.y = Mathf.MoveTowards(
                    _frameVelocity.y,
                    -maxFallSpeed,
                    inAirGravity * Time.fixedDeltaTime
                );
            }
        }

        #endregion

        private void ApplyMovement()
        {
            _rb.linearVelocity = _frameVelocity;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
            {
                Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
            }
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public Vector2 FrameInput { get; }
        public bool WallJumpUnlocked { get; }
        public bool IsTouchingWall { get; }
        public int WallDirection { get; }
    }
}
