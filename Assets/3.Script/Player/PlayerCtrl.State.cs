using UnityEngine;

public enum PlayerState
{
    Normal,      // 지상/공중 일반 이동
    Climbing,    // 벽 매달림
    Dashing,
    WallJumping, // 벽점프 직후 이동 입력 잠금
    BashAim,     // 슬로모션 조준 중
    BashBurst,   // 바쉬 발사 중
    KnockedBack,
    Dead,        // 사망 (되돌아가는 상태 없음, 씬 재로드로만 복구)
}

// 상태 전환 규칙은 이 파일에서만 관리한다.
public partial class PlayerCtrl
{
    private PlayerState state = PlayerState.Normal;
    private float stateTimer;
    private float dashDirection;
    private Vector2 bashDirection;

    public PlayerState State => state;

    // ---- 상태별 허용 동작 ----
    private bool CanMove => state == PlayerState.Normal;
    private bool CanFlip => state is PlayerState.Normal or PlayerState.WallJumping or PlayerState.KnockedBack;
    private bool CanJump => state is PlayerState.Normal or PlayerState.WallJumping;
    private bool CanDash => state is PlayerState.Normal or PlayerState.WallJumping or PlayerState.KnockedBack;
    private bool CanStartBash => state is PlayerState.Normal or PlayerState.KnockedBack;
    private bool CanBeKnockedBack => state is PlayerState.Normal or PlayerState.WallJumping;

    private void ChangeState(PlayerState next)
    {
        if (state == next) return;

        ExitState(state);
        state = next;
        stateTimer = 0f;
        EnterState(next);
    }

    private void EnterState(PlayerState next)
    {
        switch (next)
        {
            case PlayerState.Climbing:
                currentJumpCount = 0;
                animator.SetBool(AnimIsClimbing, true);
                break;

            case PlayerState.Dashing:
                SetDashTrails(true);
                dashStartTime = Time.time;
                animator.SetBool(AnimIsDash, true);
                dashDirection = FacingDirection; // 대시 시작 순간의 방향으로 고정
                rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
                break;

            case PlayerState.BashAim:
                Time.timeScale = bashSlowScale;
                arrowSprite.gameObject.SetActive(true);
                playerHealth?.SetInvincible(true);
                break;

            case PlayerState.BashBurst:
                currentJumpCount = 0; // 점프 리필
                animator.SetTrigger(AnimBash);
                SetDashTrails(true);

                Vector2 dir = bashDirection;
                if (dir.y > 0f)
                    dir.y *= bashHeightReduction; // 위쪽 성분만 완화, 수평/아래는 그대로
                rb.linearVelocity = dir * bashForce;
                break;
        }
    }

    private void ExitState(PlayerState prev)
    {
        switch (prev)
        {
            case PlayerState.Climbing:
                animator.SetBool(AnimIsClimbing, false);
                break;

            case PlayerState.Dashing:
                animator.SetBool(AnimIsDash, false);
                ScheduleTrailsOff();
                break;

            case PlayerState.BashAim:
                Time.timeScale = 1f;
                arrowSprite.gameObject.SetActive(false);
                break;

            case PlayerState.BashBurst:
                ScheduleTrailsOff();
                maxbashtime = 1f;
                playerHealth?.SetInvincible(false);
                break;
        }
    }

    // 시간 기반 상태의 종료 판정. 입력 잠금과 무관하게 매 프레임 진행한다.
    private void TickState()
    {
        switch (state)
        {
            case PlayerState.Dashing:
                stateTimer += Time.deltaTime;
                if (stateTimer >= dashDuration)
                    ChangeState(PlayerState.Normal);
                else
                    rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f); // 대시 중엔 중력 무시
                break;

            case PlayerState.WallJumping:
                stateTimer += Time.deltaTime;
                if (stateTimer >= wallJumpLockDuration)
                    ChangeState(PlayerState.Normal);
                break;

            case PlayerState.BashBurst:
                stateTimer += Time.deltaTime;
                if (stateTimer >= bashDuration)
                    ChangeState(PlayerState.Normal);
                break;

            case PlayerState.KnockedBack:
                stateTimer += Time.deltaTime;
                if (stateTimer >= knockbackLockDuration)
                    ChangeState(PlayerState.Normal);
                break;
        }
    }
}
