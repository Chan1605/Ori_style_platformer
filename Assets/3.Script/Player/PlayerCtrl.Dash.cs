using UnityEngine;

// 대시/바쉬/슈퍼점프 공용 트레일 제어 (대시 자체는 PlayerCtrl.State.cs 의 Dashing 상태)
// 끄는 시각을 하나만 관리해서, 이전 요청이 새로 켠 트레일을 조기에 끄지 못하게 한다.
public partial class PlayerCtrl
{
    private bool trailsActive;
    private float trailOffTime;

    private void SetDashTrails(bool active)
    {
        trailsActive = active;
        trailOffTime = float.PositiveInfinity; // 켜는 순간 예약된 끄기는 취소

        for (int i = 0; i < dashTrails.Length; i++)
            dashTrails[i].SetActive(active);
    }

    // dashDuration 뒤에 트레일을 끈다. 마지막 호출이 우선한다.
    private void ScheduleTrailsOff()
    {
        trailOffTime = Time.time + dashDuration;
    }

    private void TickTrails()
    {
        if (trailsActive && Time.time >= trailOffTime)
            SetDashTrails(false);
    }
}
