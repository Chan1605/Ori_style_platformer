using UnityEngine;


// 기능별 로직은 PlayerCtrl.*.cs (partial) 로 분리
public partial class PlayerCtrl : MonoBehaviour
{
    [SerializeField] private Transform modelTransform;
    [Header("---- 이동속도 ----")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float midairAcceleration = 5f; // 공중에서 방향 전환 시 가속되는 속도
    private float h;
    private float v;
    private float previousMoveDir = 1f;
    private float accelerationFactor = 1f;

    [Header("---- 점프 ----")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float secondJumpForce = 10f;
    [SerializeField] private float jumpHoldTime = 0.2f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private float baseGravityForce = 1f;
    [SerializeField] private float fallGravityForce = 2.2f; // 낙하 중엔 중력 더 받게
    private int currentJumpCount;
    private bool isGrounded;
    private bool isJumpHeld;
    private float jumpHoldTimer;
    private int jumpCounter = 0;
    [Header("---- 빠른 낙하 / 발판 통과 ----")]
    [SerializeField] private float fastFallSpeed = 12f;
    [SerializeField] private float dropThroughDuration = 0.3f; // 이 시간 동안 Ground와 충돌 무시
    [SerializeField] private float dropCheckDistance = 15f;     // 이 거리 안에 내려갈 지형이 있는지 확인
    [SerializeField] private float hopForce = 4f;               // 내려갈 곳 없을 때 제자리 점프

    [Header("---- 슈퍼 점프 ----")]
    [SerializeField] private float superJumpForce = 15f;
    [SerializeField] private float particleDelay = 1.5f;
    [SerializeField] private float minSuperJumpHoldTime = 1.5f;
    private float superJumpChargeTimer;
    private bool isChargingSuperJump;
    private bool hasPlayedChargeParticle;

    [Header("---- 대시 ----")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashCooldown = 1f;
    private float dashStartTime = -100f;

    [Header("---- 패러세일 ----")]
    [SerializeField] private float hoverFallSpeedLimit = 3f;
    private bool isHovering;
    private bool isHoverHeld;

    [Header("---- 벽타기 ----")]
    [SerializeField] private Transform climbCheck;
    [SerializeField] private float climbCheckRadius = 0.3f;
    [SerializeField] private LayerMask climbableLayer;
    [SerializeField] private float climbSlideSpeed = 10f; // 벽에 매달리면 입력과 무관하게 이 속도로 미끄러짐 (의도된 동작)
    [SerializeField] private float wallJumpForceX = 1f;
    [SerializeField] private float wallJumpForceY = 15f;
    [SerializeField] private float wallJumpLockDuration = 0.2f;
    [SerializeField] private float wallInputBufferTime = 0.15f; //값이 클수록 붙기 쉬워짐
    [SerializeField] private float wallCoyoteTime = 0.1f; //값이 크면 허공에서 공중부양함
    private bool isTouchingWall;

    private float lastPressTowardWallTime = -10f;
    private float lastTouchWallTime = -10f;
    [Header("---- 피격 ----")]
    [SerializeField] private LayerMask enemyContactLayer;
    [SerializeField] private float knockbackForceX = 6f;
    [SerializeField] private float knockbackForceY = 4f;
    [SerializeField] private float knockbackLockDuration = 0.2f;

    [Header("---- UI 및 효과 ----")]
    public RectTransform arrowSprite;
    public GameObject[] dashTrails;
    public ParticleSystem SuperjumpParticle;

    [Header("---- 바쉬 ----")]
    [SerializeField] private KeyCode bashKey = KeyCode.Mouse1; // 우클릭
    [SerializeField] private float bashForce = 20f;
    [SerializeField] private float bashDuration = 0.15f;
    [SerializeField] private float bashSlowScale = 0.25f; // 조준 중 슬로우모션 배율
    [SerializeField] private float bashHeightReduction = 0.5f; // 위쪽 바쉬 시 y 완화
    private Camera cam;
    [Header("---- 바쉬 화살표 ----")]
    [SerializeField] private Vector3 arrowWorldOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float arrowDistanceFromPlayer = 1.2f;
    [Header("---- 바쉬 조건 ----")]
    [SerializeField] private float bashDetectRadius = 4f;
    [SerializeField] private LayerMask bashTargetLayer;
    [SerializeField] private bool isDevelop; //개발용
    [SerializeField] private float maxbashtime = 2f;

    public bool IsClimbing => state == PlayerState.Climbing; //카메라 제어용
    public bool IsDashing => state == PlayerState.Dashing;
    public bool IsBashing => state is PlayerState.BashAim or PlayerState.BashBurst;
    public bool IsGrounded => isGrounded;
    public bool IsDie => state == PlayerState.Dead;
    public float FacingDirection { get; private set; } = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerHealth playerHealth;

    // ---- 상수 ----
    private const KeyCode SuperJumpKey = KeyCode.W;
    private const float DropMinGroundDistance = 0.3f; // 이보다 가까운 지형은 "내려갈 곳"으로 치지 않음
    private const float FaceAngleY = 90f;

    // ---- 애니메이터 파라미터 해시 ----
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimIsLookingUp = Animator.StringToHash("IsLookingUp");
    private static readonly int AnimIsHovering = Animator.StringToHash("IsHovering");
    private static readonly int AnimIsClimbing = Animator.StringToHash("IsClimbing");
    private static readonly int AnimIsDash = Animator.StringToHash("IsDash");
    private static readonly int AnimJump = Animator.StringToHash("Jump");
    private static readonly int AnimJumpVariation = Animator.StringToHash("jumpVariation");
    private static readonly int AnimSecondJump = Animator.StringToHash("secondJump");
    private static readonly int AnimBash = Animator.StringToHash("Bash");

    private void Start()
    {
        TryGetComponent(out rb);
        TryGetComponent(out animator);
        TryGetComponent(out playerHealth);
        cam = Camera.main;
    }

    private void Update()
    {
        TickState();
        TickTrails();

        if (GameManager.inst.IsInputLocked)
            return;

        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        UpdateAnimator();

        bool pressingTowardWallNow = (FacingDirection > 0f && h > 0f) || (FacingDirection < 0f && h < 0f);
        if (pressingTowardWallNow) lastPressTowardWallTime = Time.time;

        CheckWall();
        if (isTouchingWall) lastTouchWallTime = Time.time;

        if (CanFlip)
            FlipModel();

        UpdateClimbState();
        UpdateClimbCheckPosition();

        HandleBashInput();
        HandleJumpInput();
        HandleDashInput();
        HandleSuperJumpInput();
        UpdateHover();
    }

    private void FixedUpdate()
    {
        if (CanMove)
            Move();

        CheckGround();
        ApplyJumpHold();
        ApplyGravityScale();
        ApplyClimbVelocity();
        ApplyHoverLimit();
        ApplyFastFall();
    }

    private void HandleJumpInput()
    {
        if (!Input.GetButtonDown("Jump"))
            return;

        if (state == PlayerState.Climbing)
        {
            WallJump();
            return;
        }

        if (!CanJump) return;

        if (isGrounded && v < 0f)
            TryDropThrough();
        else if (currentJumpCount < maxJumpCount)
            Jump();
    }

    private void HandleDashInput()
    {
        if (Input.GetKeyDown(dashKey) && CanDash && Time.time - dashStartTime > dashCooldown)
            ChangeState(PlayerState.Dashing);
    }

    private void UpdateHover()
    {
        isHoverHeld = Input.GetButton("Hover");
        isHovering = isHoverHeld && !isGrounded && rb.linearVelocity.y < 0f;
        animator.SetBool(AnimIsHovering, isHovering);
    }

    // 낙하 중일 때만 중력을 더 세게
    private void ApplyGravityScale()
    {
        if (state == PlayerState.Climbing) return; // 클라임 중엔 직접 제어

        rb.gravityScale = rb.linearVelocity.y < 0f ? baseGravityForce * fallGravityForce : baseGravityForce;
    }

    private void ApplyHoverLimit()
    {
        if (!isHovering) return;

        animator.SetBool(AnimIsHovering, isHoverHeld);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -hoverFallSpeedLimit));
    }

    private void ApplyFastFall()
    {
        if (isGrounded || v >= 0f || state == PlayerState.Climbing) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, -fastFallSpeed));
    }

    private void FlipModel()
    {
        if (h > 0f)
            SetFacing(1f);
        else if (h < 0f)
            SetFacing(-1f);
    }

    private void SetFacing(float sign)
    {
        modelTransform.localEulerAngles = new Vector3(0f, sign * FaceAngleY, 0f);
        FacingDirection = sign;
    }

    private void Move()
    {
        float currentMoveDir = h > 0f ? 1f : (h < 0f ? -1f : previousMoveDir);

        // 공중에서 방향이 바뀌면 가속도를 0부터 다시 쌓기 시작 -> 미끄러지듯 부드러운 전환
        if (!isGrounded && currentMoveDir != previousMoveDir && h != 0f)
        {
            accelerationFactor = 0f;
        }
        else if (isGrounded)
        {
            accelerationFactor = 1f; // 지상에서는 항상 즉각 반응
        }

        float targetX = h * moveSpeed * accelerationFactor;
        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);

        if (!isGrounded)
        {
            accelerationFactor = Mathf.Clamp(accelerationFactor + midairAcceleration * Time.fixedDeltaTime, 0f, 1f);
        }

        previousMoveDir = currentMoveDir;
    }

    private void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (!wasGrounded && isGrounded)
            currentJumpCount = 0;

        animator.SetBool(AnimIsGrounded, isGrounded);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool(AnimIsMoving, h != 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isEnemy = ((1 << other.gameObject.layer) & enemyContactLayer) != 0;
        if (!isEnemy) return;

        Vector2 dir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TryStartKnockback(dir, 1f, allowRestart: true);

        int damage = 1;
        if (other.TryGetComponent(out EnemyBase enemy))
            damage = enemy.Damage;

        playerHealth?.TakeDamage(damage);
    }

    public void Kill()
    {
        ChangeState(PlayerState.Dead); // 진행 중이던 상태의 퇴장 처리(슬로모션/무적/애니메이터 복구)
        SetDashTrails(false);          // 이후 이 컴포넌트가 비활성화되어 TickTrails 가 돌지 않으므로 즉시 끈다
    }

}
