using System.Collections;
using UnityEngine;

// 일반 점프 / 슈퍼 점프 / 발판 통과
public partial class PlayerCtrl
{
    private void Jump()
    {
        currentJumpCount++;
        isJumpHeld = true;
        jumpHoldTimer = 0f;

        if (currentJumpCount == 1)
        {
            jumpCounter++;
            int jumpTrigger = jumpCounter % 3 == 0 ? AnimJumpVariation : AnimJump;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetTrigger(jumpTrigger);
        }
        else if (currentJumpCount == 2)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, secondJumpForce);
            animator.SetTrigger(AnimSecondJump);
        }
    }

    private void ApplyJumpHold()
    {
        if (!isJumpHeld || !Input.GetButton("Jump"))
            return;

        jumpHoldTimer += Time.fixedDeltaTime;
        if (jumpHoldTimer < jumpHoldTime)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        else
            isJumpHeld = false;
    }

    #region SuperJump

    private void HandleSuperJumpInput()
    {
        if (isGrounded)
        {
            if (Input.GetKeyDown(SuperJumpKey))
            {
                isChargingSuperJump = true;
                superJumpChargeTimer = 0f;
                hasPlayedChargeParticle = false;
                animator.SetBool(AnimIsLookingUp, true);
            }

            if (isChargingSuperJump && Input.GetKey(SuperJumpKey))
            {
                superJumpChargeTimer += Time.deltaTime;

                if (!hasPlayedChargeParticle && superJumpChargeTimer >= particleDelay)
                {
                    SuperjumpParticle.Play();
                    hasPlayedChargeParticle = true;
                }
            }

            if (Input.GetKeyUp(SuperJumpKey))
            {
                animator.SetBool(AnimIsLookingUp, false);
                isChargingSuperJump = false;

                if (superJumpChargeTimer >= minSuperJumpHoldTime)
                    SuperJump();

                superJumpChargeTimer = 0f;
                if (!IsDashing && !IsBashing) // 대시/바쉬 중이면 해당 상태 퇴장 시 예약됨
                    ScheduleTrailsOff();
            }
        }
        else if (isChargingSuperJump)
        {
            isChargingSuperJump = false;
            superJumpChargeTimer = 0f;
            animator.SetBool(AnimIsLookingUp, false);
        }
    }

    private void SuperJump()
    {
        currentJumpCount = 1;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, superJumpForce);
        SetDashTrails(true);
        ScheduleTrailsOff();
    }

    #endregion

    #region DropThrough

    private void TryDropThrough()
    {
        // 바로 아래 내려갈 지형이 있는지 확인
        RaycastHit2D[] hits = Physics2D.RaycastAll(groundCheck.position, Vector2.down, dropCheckDistance, groundLayer);

        bool hasLowerGround = false;
        foreach (var hit in hits)
        {
            if (hit.distance > DropMinGroundDistance)
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
            animator.SetTrigger(AnimJump);
        }
    }

    private IEnumerator DropThroughRoutine()
    {
        SetLayerCollisionIgnore(groundLayer, true);
        yield return new WaitForSeconds(dropThroughDuration);
        SetLayerCollisionIgnore(groundLayer, false);
    }

    private void SetLayerCollisionIgnore(LayerMask mask, bool ignore)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0) // 이 마스크에 포함된 레이어인지 하나씩 확인
                Physics2D.IgnoreLayerCollision(gameObject.layer, i, ignore);
        }
    }

    #endregion
}
