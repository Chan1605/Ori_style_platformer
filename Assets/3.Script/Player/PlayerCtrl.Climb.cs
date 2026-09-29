using UnityEngine;

// 벽타기 / 벽점프
public partial class PlayerCtrl
{
    private void CheckWall()
    {
        isTouchingWall = Physics2D.OverlapCircle(climbCheck.position, climbCheckRadius, climbableLayer);
    }

    private void UpdateClimbState()
    {
        bool pressBuffered = Time.time - lastPressTowardWallTime <= wallInputBufferTime;
        bool touchBuffered = Time.time - lastTouchWallTime <= wallCoyoteTime;

        if (state == PlayerState.Normal && touchBuffered && pressBuffered && !isGrounded)
            ChangeState(PlayerState.Climbing);

        if (state == PlayerState.Climbing)
        {
            bool pressingAway = (FacingDirection > 0f && h < 0f) || (FacingDirection < 0f && h > 0f);
            if (!isTouchingWall || isGrounded || pressingAway)
                ChangeState(PlayerState.Normal);
        }
    }

    private void ApplyClimbVelocity()
    {
        if (state != PlayerState.Climbing) return;

        // 매달린 상태에서는 입력과 무관하게 항상 미끄러진다
        rb.linearVelocity = new Vector2(0f, -climbSlideSpeed);
    }

    private void WallJump()
    {
        ChangeState(PlayerState.WallJumping); // 벽타기 상태 퇴장 처리 포함
        animator.SetTrigger(AnimJump);

        float jumpDir = -FacingDirection;
        rb.linearVelocity = new Vector2(jumpDir * wallJumpForceX, wallJumpForceY);
        currentJumpCount = 1;
    }

    private void UpdateClimbCheckPosition()
    {
        Vector3 pos = climbCheck.localPosition;
        climbCheck.localPosition = new Vector3(Mathf.Abs(pos.x) * FacingDirection, pos.y, pos.z);
    }
}
