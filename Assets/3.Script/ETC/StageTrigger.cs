using System.Collections;
using UnityEngine;

public class StageTrigger : MonoBehaviour
{
    [SerializeField] private string stageName;
    [SerializeField] private string stageinfo;
    [SerializeField] private bool showBossReveal = false;
    [SerializeField] private EnemySpawner[] activateOnEnter;
    [SerializeField] private EnemySpawner[] deactivateOnEnter;
    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;

        foreach (var spawner in activateOnEnter)
            spawner?.Activate();

        foreach (var spawner in deactivateOnEnter)
            spawner?.Deactivate();

        EnterStageWhenReady();
    }

    private void EnterStageWhenReady()
    {
        // 이미 키가이드를 본 상태면 코루틴/이벤트 대기 없이 즉시 진입
        if (GameManager.inst == null || GameManager.inst.HasSeenKeyGuide)
        {
            StageManager.Instance.EnterStage(stageName, stageinfo, showBossReveal);
            return;
        }

        // 아직 안 봤으면 완료 이벤트를 구독하고 대기
        GameEvents.Stage.OnKeyGuideCompleted += HandleKeyGuideCompleted;
    }

    private void HandleKeyGuideCompleted()
    {
        GameEvents.Stage.OnKeyGuideCompleted -= HandleKeyGuideCompleted;
        StageManager.Instance.EnterStage(stageName, stageinfo, showBossReveal);
    }

    private void OnDisable()
    {
        // 대기 중에 오브젝트가 비활성화/파괴되는 경우 구독 해제 (메모리 누수 방지)
        GameEvents.Stage.OnKeyGuideCompleted -= HandleKeyGuideCompleted;
    }

}