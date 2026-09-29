using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCtrl : MonoBehaviour
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
    [SerializeField] private float upJumpBoostMultiplier = 1.3f;
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
    private bool isDropping;

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
    private bool isDashing;
    private float dashStartTime = -100f;

    [Header("---- 패러세일 ----")]
    [SerializeField] private float hoverFallSpeedLimit = 3f;
    [SerializeField] private KeyCode hoverKey = KeyCode.LeftControl;
    private bool isHovering;

    [Header("---- 벽타기 ----")]
    [SerializeField] private Transform climbCheck;
    [SerializeField] private float climbCheckRadius = 0.3f;
    [SerializeField] private LayerMask climbableLayer;
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float climbSlideSpeed = 10f;
    [SerializeField] private float wallJumpForceX = 1f;
    [SerializeField] private float wallJumpForceY = 15f;
    [SerializeField] private float wallJumpLockDuration = 0.2f;
    [SerializeField] private float wallInputBufferTime = 0.15f; //값이 클수록 붙기 쉬워짐
    [SerializeField] private float wallCoyoteTime = 0.1f; //값이 크면 허공에서 공중부양함
    private bool isTouchingWall;
    private bool isClimbing;
    private bool isWallJumping;

    private float lastPressTowardWallTime = -10f;
    private float lastTouchWallTime = -10f;
    [Header("---- 피격 ----")]
    [SerializeField] private LayerMask enemyContactLayer;
    [SerializeField] private float knockbackForceX = 6f;
    [SerializeField] private float knockbackForceY = 4f;
    [SerializeField] private float knockbackLockDuration = 0.2f;
    private bool isKnockedBack;
    private bool isDie;

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
    private bool isAimingBash;
    [SerializeField] private bool isBashing;
    private Camera cam;
    [Header("---- 바쉬 화살표 ----")]
    [SerializeField] private Vector3 arrowWorldOffset = new Vector3(0f, 1f, 0f); 
    [SerializeField] private float arrowDistanceFromPlayer = 1.2f;
    //[SerializeField] private float arrowRotationOffset = 0f; // 화살표 이미지 방향 보정용
    [Header("---- 바쉬 조건 ----")]
    [SerializeField] private float bashDetectRadius = 4f;
    [SerializeField] private LayerMask bashTargetLayer;
    [SerializeField] private bool isDevelop; //개발용
    [SerializeField] private float maxbashtime = 2f;
    public bool IsClimbing => isClimbing; //카메라 제어용
    public bool IsDashing => isDashing;
    public bool IsBashing => isBashing;
    public bool IsGrounded => isGrounded;
    public bool IsDie => isDie;
    
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerHealth playerHealth;
    public float FacingDirection { get; private set; } = 1f;

 

    void Start()
    {
        TryGetComponent(out rb);
        TryGetComponent(out animator);
        TryGetComponent(out playerHealth);
        cam = Camera.main;
    }

    void Update()
    {
        if (GameManager.inst.IsInputLocked)
            return;

        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        UpdateAnimator();
        bool pressingTowardWallNow = (FacingDirection > 0f && h > 0f) || (FacingDirection < 0f && h < 0f);
        if (pressingTowardWallNow) lastPressTowardWallTime = Time.time;

        CheckWall();
        if (isTouchingWall) lastTouchWallTime = Time.time;
        if (!isDashing && !isClimbing && !isBashing)
        {
            FlipModel();
        }
        UpdateClimbState();
        UpdateClimbCheckPosition();

        if (Input.GetKeyDown(bashKey) && !isDashing && !isClimbing && !isWallJumping && !isBashing && (HasBashTarget() || isDevelop))
        {
            StartBashAim();
        }

        if (isAimingBash )
        {
            maxbashtime -= Time.unscaledDeltaTime;
            UpdateBashAim();

            if (Input.GetKeyUp(bashKey) || maxbashtime <= 0f)
            {
                maxbashtime = 0f;
         
                ReleaseBash();
            }
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (isClimbing)
            {
                WallJump();
            }
            else if (isGrounded && v < 0f)
            {
                TryDropThrough();
            }
            else if (currentJumpCount < maxJumpCount)
            {
                Jump();
            }
        }

        if (Input.GetKeyDown(dashKey) && !isDashing && !isClimbing && Time.time - dashStartTime > dashCooldown)
        {
            StartCoroutine(DashRoutine());
        }


        if (isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                isChargingSuperJump = true;
                superJumpChargeTimer = 0f;
                hasPlayedChargeParticle = false;
                animator.SetBool("IsLookingUp", true);
            }

            if (isChargingSuperJump && Input.GetKey(KeyCode.W))
            {
                superJumpChargeTimer += Time.deltaTime;

                if (!hasPlayedChargeParticle && superJumpChargeTimer >= particleDelay)
                {
                    SuperjumpParticle.Play();
                    hasPlayedChargeParticle = true;
                }
            }

            if (Input.GetKeyUp(KeyCode.W))
            {
                animator.SetBool("IsLookingUp", false);
                isChargingSuperJump = false;
                //if (superJumpChargeTimer >= superJumpHoldThreshold) //자동점프 삭제
                //{
                //    SuperJump();
                //}

                if (superJumpChargeTimer >= minSuperJumpHoldTime) 
                {
                    SuperJump();
                }
                superJumpChargeTimer = 0f;
                StartCoroutine(StopDashLines());
            }
        }
        else if (isChargingSuperJump)
        {
            isChargingSuperJump = false;
            superJumpChargeTimer = 0f;
            animator.SetBool("IsLookingUp", false);
        }


        bool holdingHoverKey = Input.GetButton("Hover");
        isHovering = holdingHoverKey && !isGrounded && rb.linearVelocity.y < 0f;
        animator.SetBool("IsHovering", isHovering);

    }

    void FixedUpdate()
    {
        if (!isDashing && !isClimbing && !isWallJumping && !isKnockedBack && !isBashing)
        {
            Move();
        }

        CheckGround();
        ApplyJumpHold();
        // 낙하 중일 때만 중력을 더 세게
        if (!isClimbing) // 클라임 중엔 직접 제어
        {
            rb.gravityScale = rb.linearVelocity.y < 0f ? baseGravityForce * fallGravityForce : baseGravityForce;
        }

        if (isClimbing)
        {
            float climbVelY;
            if (h != 0f || v != 0f) //대각선 방향 입력 시 벽타기 방지
            {

                climbVelY = -climbSlideSpeed;
            }
            else
            {
                // 순수하게 위/아래만 눌렀을 때만 진짜로 오르내림
                climbVelY = v != 0f ? v * climbSpeed : -climbSlideSpeed;
            }
            rb.linearVelocity = new Vector2(0f, climbVelY);
        }

        if (isHovering)
        {
            bool holdingHoverKey = Input.GetButton("Hover");
            animator.SetBool("IsHovering", holdingHoverKey);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -hoverFallSpeedLimit));
        }

        if (!isGrounded && v < 0f && !isClimbing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, -fastFallSpeed));
        }
    }

    private void FlipModel()
    {
        if (h > 0f)
        {
            modelTransform.localEulerAngles = new Vector3(0f, 90f, 0f);
            FacingDirection = 1f;
        }
        else if (h < 0f)
        {
            modelTransform.localEulerAngles = new Vector3(0f, -90f, 0f);
            FacingDirection = -1f;
        }
    }


    private void Move()
    {
        if (isDashing)
            return;

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
        {
            currentJumpCount = 0;
        }

        animator.SetBool("IsGrounded", isGrounded);
    }
    private void UpdateAnimator()
    {
        if (animator == null) return;
        bool isMoving = (h != 0f);
        animator.SetBool("IsMoving", isMoving);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isEnemy = ((1 << other.gameObject.layer) & enemyContactLayer) != 0;
        if (!isEnemy) return;

        Vector2 dir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        StartCoroutine(KnockbackRoutine(dir));


        int damage = 1;
        if (other.TryGetComponent(out EnemyBase enemy))
        {
            damage = enemy.Damage;
        }

        playerHealth?.TakeDamage(damage);
    }

    public void Kill()
    {
        isDie = true;
    }

    #region Jump

    private void Jump()
    {
        currentJumpCount++;
        isJumpHeld = true;
        jumpHoldTimer = 0f;

        if (currentJumpCount == 1)
        {
            jumpCounter++;
            string jumpName = jumpCounter % 3 == 0 ? "jumpVariation" : "Jump";
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetTrigger(jumpName);
        }
        else if (currentJumpCount == 2)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, secondJumpForce);
            animator.SetTrigger("secondJump");
        }
    }

    private void ApplyJumpHold()
    {
        if (isJumpHeld && Input.GetButton("Jump"))
        {
            jumpHoldTimer += Time.fixedDeltaTime;
            if (jumpHoldTimer < jumpHoldTime)
            {

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else
            {
                isJumpHeld = false;
            }
        }

    }

    private void SuperJump()
    {
        currentJumpCount = 1;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, superJumpForce);
        for (int i = 0; i < dashTrails.Length; i++)
        {
            dashTrails[i].SetActive(true);
        }

        StartCoroutine(StopDashLines());
    }

    #endregion

    #region Dash
    private IEnumerator DashRoutine()
    {
        isDashing = true;
        for (int i = 0; i < dashTrails.Length; i++)
        {
            dashTrails[i].SetActive(true);
        }
        dashStartTime = Time.time;
        animator.SetBool("IsDash", true);

        float dashDir = FacingDirection; // 대시 시작 순간의 방향으로 고정

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f); // 대시 중엔 중력 무시
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        animator.SetBool("IsDash", false);
        StartCoroutine(StopDashLines());
    }

    private IEnumerator StopDashLines()
    {
        yield return new WaitForSeconds(dashDuration);
        for (int i = 0; i < dashTrails.Length; i++)
        {
            dashTrails[i].SetActive(false);
        }
    }

    #endregion

    #region Climbing


    private void CheckWall()
    {
        isTouchingWall = Physics2D.OverlapCircle(climbCheck.position, climbCheckRadius, climbableLayer);

    }

    private void UpdateClimbState()
    {
        bool pressBuffered = Time.time - lastPressTowardWallTime <= wallInputBufferTime;
        bool touchBuffered = Time.time - lastTouchWallTime <= wallCoyoteTime;

        if (!isClimbing && touchBuffered && pressBuffered && !isGrounded && !isWallJumping)
        {

            isClimbing = true;
            currentJumpCount = 0;
            animator.SetBool("IsClimbing", true);
        }

        if (isClimbing)
        {
            bool pressingAway = (FacingDirection > 0f && h < 0f) || (FacingDirection < 0f && h > 0f);
            if (!isTouchingWall || isGrounded || pressingAway)
            {
                isClimbing = false;
                animator.SetBool("IsClimbing", false);
            }
        }
    }

    private void WallJump()
    {
        isClimbing = false;

        animator.SetBool("IsClimbing", false);
        animator.SetTrigger("Jump");

        float jumpDir = -FacingDirection;
        rb.linearVelocity = new Vector2(jumpDir * wallJumpForceX, wallJumpForceY);
        currentJumpCount = 1;

        StartCoroutine(WallJumpLockRoutine());
    }

    private IEnumerator WallJumpLockRoutine()
    {
        isWallJumping = true;
        yield return new WaitForSeconds(wallJumpLockDuration);
        isWallJumping = false;
    }

    private void UpdateClimbCheckPosition()
    {
        Vector3 pos = climbCheck.localPosition;
        climbCheck.localPosition = new Vector3(Mathf.Abs(pos.x) * FacingDirection, pos.y, pos.z);
    }

    #endregion

    #region DropJump

    private void TryDropThrough()
    {
        // 바로 아래 내려갈 지형이 있는지 확인
        RaycastHit2D[] hits = Physics2D.RaycastAll(groundCheck.position, Vector2.down, dropCheckDistance, groundLayer);

        bool hasLowerGround = false;
        foreach (var hit in hits)
        {
            if (hit.distance > 0.3f)
            {
                hasLowerGround = true;
                break;
            }
        }

        if (hasLowerGround)
        {
            StartCoroutine(DropThroughRoutine());
        }
        else
        {
            // 내려갈 곳이 없으면 제자리 점프
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, hopForce);
            animator.SetTrigger("Jump");
        }
    }

    private IEnumerator DropThroughRoutine()
    {
        isDropping = true;
        SetLayerCollisionIgnore(groundLayer, true); // 기존 IgnoreLayerCollision 한 줄을 이걸로 교체

        yield return new WaitForSeconds(dropThroughDuration);

        SetLayerCollisionIgnore(groundLayer, false);
        isDropping = false;
    }

    private void SetLayerCollisionIgnore(LayerMask mask, bool ignore)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0) // 이 마스크에 포함된 레이어인지 하나씩 확인
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, i, ignore);
            }
        }
    }

    private int LayerMaskToLayer(LayerMask mask)
    {
        int layerNumber = 0;
        int layer = mask.value;
        while (layer > 1)
        {
            layer >>= 1;
            layerNumber++;
        }
        return layerNumber;
    }

    #endregion

    #region Knockback

    private IEnumerator KnockbackRoutine(Vector2 dir, float multiplier = 1f)
    {
        isKnockedBack = true;
        rb.linearVelocity = new Vector2(dir.x * knockbackForceX * multiplier, knockbackForceY * multiplier);
        yield return new WaitForSeconds(knockbackLockDuration);
        isKnockedBack = false;
    }

    public void ApplyKnockback(Vector2 dir, float multiplier = 1f)
    {
        if (isKnockedBack) return; // 이미 넉백 중이면 중첩 방지

        Vector2 scaledDir = new Vector2(dir.x, dir.y);
        StartCoroutine(KnockbackRoutine(scaledDir, multiplier));
    }
    #endregion

    #region Bash
    private void StartBashAim()
    {
        isAimingBash = true;
        isBashing = true;
        Time.timeScale = bashSlowScale;
        arrowSprite.gameObject.SetActive(true);
        playerHealth?.SetInvincible(true);
    }
    private bool HasBashTarget()
    {
        return Physics2D.OverlapCircle(transform.position, bashDetectRadius, bashTargetLayer) != null;
    }

    private void UpdateBashAim()
    {
        // 발사 때 쓰는 것과 완전히 같은 방식으로 월드 좌표 방향 계산 (좌표계 불일치 제거)
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -cam.transform.position.z));
        Vector2 worldDir = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        // 캐릭터 위치 + 오프셋 + 조준 방향으로 살짝 띄운 지점을 화살표 위치로
        Vector3 arrowWorldPos = transform.position + arrowWorldOffset + (Vector3)(worldDir * arrowDistanceFromPlayer);
        Vector3 screenPos = cam.WorldToScreenPoint(arrowWorldPos);

        float angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg; //+ arrowRotationOffset;

        arrowSprite.position = screenPos;
        arrowSprite.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    private void ReleaseBash()
    {
        isAimingBash = false;
        Time.timeScale = 1f;
        arrowSprite.gameObject.SetActive(false);

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -cam.transform.position.z));
        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        FaceBashDirection(direction); // 방향 결정 직후, 코루틴 시작 전에 호출

        StartCoroutine(BashRoutine(direction));
    }

    private void FaceBashDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) < 0.01f) 
            return; // 거의 수직으로 쏘면 기존 방향 유지

        float sign = dir.x > 0f ? 1f : -1f;
        modelTransform.localEulerAngles = new Vector3(0f, sign > 0f ? 90f : -90f, 0f);
        FacingDirection = sign;
    }

    private IEnumerator BashRoutine(Vector2 direction)
    {
        currentJumpCount = 0; //점프 리필
        animator.SetTrigger("Bash");

        for(int i=0; i<dashTrails.Length; i++)
        {
            dashTrails[i].SetActive(true);
        }

        if (direction.y > 0f)
        {
            direction.y *= bashHeightReduction; // 위쪽 성분만 완화, 수평/아래는 그대로
        }

        rb.linearVelocity = direction * bashForce;

        yield return new WaitForSeconds(bashDuration);
        StartCoroutine(StopDashLines());
        isBashing = false;
        maxbashtime = 1f;
        playerHealth?.SetInvincible(false);
    }
    #endregion
}