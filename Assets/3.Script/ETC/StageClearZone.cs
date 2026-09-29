using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StageClearZone : MonoBehaviour
{
    [SerializeField] private string stageGroupId = "Stage1";
    [SerializeField] private int requiredKills = 10;
    [SerializeField] private GameObject invisibleWall;
    [SerializeField] private Text progressText;
    [SerializeField] private Text clearMessageText;
    [SerializeField] private string clearMessage = "Stage1 클리어"; 
    [SerializeField] private float clearMessageDuration = 3f;

    private bool cleared;

    private void Start()
    {
        int savedKills = GameManager.inst != null ? GameManager.inst.GetKillCount(stageGroupId) : 0;

        if (progressText != null)
            progressText.gameObject.SetActive(savedKills > 0); // 잡은 게 있으면 복원 시에도 바로 노출

        UpdateProgressUI(savedKills);

        if (clearMessageText != null)
            clearMessageText.gameObject.SetActive(false);

        if (savedKills >= requiredKills)
        {
            ClearStage(instant: true); // 복원된 상태면 안내문구/딜레이 없이 조용히 벽만 해제
        }
    }

    private void OnEnable() => EnemyBase.OnAnyEnemyDeath += HandleEnemyDeath;
    private void OnDisable() => EnemyBase.OnAnyEnemyDeath -= HandleEnemyDeath;

    private void HandleEnemyDeath(EnemyBase enemy)
    {
        if (cleared) return;
        if (enemy.StageGroupId != stageGroupId) return;

        GameManager.inst?.AddKillCount(stageGroupId, 1);
        int kills = GameManager.inst != null ? GameManager.inst.GetKillCount(stageGroupId) : 0;

        if (progressText != null && !progressText.gameObject.activeSelf)
            progressText.gameObject.SetActive(true); // 첫 처치 후 활성화

        UpdateProgressUI(kills);

        if (kills >= requiredKills)
        {
            ClearStage(instant: false);
        }
    }

    private void UpdateProgressUI(int kills)
    {
        if (progressText != null)
            progressText.text = $"{kills} / {requiredKills}";
    }

    private void ClearStage(bool instant)
    {
        cleared = true;

        if (invisibleWall != null && invisibleWall.TryGetComponent(out Collider2D col))
            col.enabled = false;

        if (progressText != null)
            progressText.gameObject.SetActive(false);

        if (clearMessageText != null && !instant)
        {
            clearMessageText.text = clearMessage;
            clearMessageText.gameObject.SetActive(true);
            StartCoroutine(HideClearMessageAfterDelay());
        }
    }

    private IEnumerator HideClearMessageAfterDelay()
    {
        yield return new WaitForSeconds(clearMessageDuration);
        if (clearMessageText != null)
            clearMessageText.gameObject.SetActive(false);
    }
}