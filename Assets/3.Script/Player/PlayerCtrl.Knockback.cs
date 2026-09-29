using UnityEngine;

// 피격 넉백 (지속 시간은 PlayerCtrl.State.cs 의 KnockedBack 상태)
public partial class PlayerCtrl
{
    // allowRestart: 이미 넉백 중일 때 타이머/속도를 다시 적용할지
    private bool TryStartKnockback(Vector2 dir, float multiplier, bool allowRestart)
    {
        bool alreadyKnockedBack = state == PlayerState.KnockedBack;
        if (alreadyKnockedBack ? !allowRestart : !CanBeKnockedBack)
            return false;

        rb.linearVelocity = new Vector2(dir.x * knockbackForceX * multiplier, knockbackForceY * multiplier);

        if (alreadyKnockedBack)
            stateTimer = 0f;
        else
            ChangeState(PlayerState.KnockedBack);

        return true;
    }

    public void ApplyKnockback(Vector2 dir, float multiplier = 1f)
    {
        TryStartKnockback(dir, multiplier, allowRestart: false); // 이미 넉백 중이면 중첩 방지
    }
}
