using UnityEngine;

// 바쉬 조준 입력 / 화살표 (상태 진입·종료는 PlayerCtrl.State.cs 의 BashAim, BashBurst)
public partial class PlayerCtrl
{
    private void HandleBashInput()
    {
        if (Input.GetKeyDown(bashKey) && CanStartBash && (HasBashTarget() || isDevelop))
            ChangeState(PlayerState.BashAim);

        if (state != PlayerState.BashAim) return;

        maxbashtime -= Time.unscaledDeltaTime;
        UpdateBashAim();

        if (Input.GetKeyUp(bashKey) || maxbashtime <= 0f)
        {
            maxbashtime = 0f;
            ReleaseBash();
        }
    }

    private bool HasBashTarget()
    {
        return Physics2D.OverlapCircle(transform.position, bashDetectRadius, bashTargetLayer) != null;
    }

    // 조준/발사 모두 같은 월드 좌표 기준으로 방향을 계산 (좌표계 불일치 방지)
    private Vector2 GetMouseAimDirection()
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -cam.transform.position.z));
        return ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;
    }

    private void UpdateBashAim()
    {
        Vector2 worldDir = GetMouseAimDirection();

        // 캐릭터 위치 + 오프셋 + 조준 방향으로 살짝 띄운 지점을 화살표 위치로
        Vector3 arrowWorldPos = transform.position + arrowWorldOffset + (Vector3)(worldDir * arrowDistanceFromPlayer);
        float angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg;

        arrowSprite.position = cam.WorldToScreenPoint(arrowWorldPos);
        arrowSprite.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ReleaseBash()
    {
        bashDirection = GetMouseAimDirection();

        FaceBashDirection(bashDirection); // 방향 결정 직후, 발사 전에 호출
        ChangeState(PlayerState.BashBurst); // BashAim 퇴장 시 슬로모션/화살표 복구
    }

    private void FaceBashDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) < 0.01f)
            return; // 거의 수직으로 쏘면 기존 방향 유지

        SetFacing(dir.x > 0f ? 1f : -1f);
    }
}
