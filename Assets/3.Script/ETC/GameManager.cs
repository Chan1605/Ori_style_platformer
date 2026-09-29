using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager inst;
    public GameObject PausePanel;
    public GameObject EndPanel;
    public Button RetryBtn;
    public Button LastCheckBtn;

    [Header("---- 체크포인트 ----")]
    public Vector3 lastCheckpointPosition;
    public bool hasCheckpoint;
    private bool spawnAtCheckpoint;
    private int savedHealth;
    private int savedSkillGauge;
    private int savedCheckpointGauge;
    private int savedAccumulatedSouls;

    private bool isesc = false;
    public bool IsRespawningFromCheckpoint { get; private set; }
    public bool HasSeenKeyGuide { get; private set; }
    [Header("---- 프리즈 잠금 ----")]
    private int freezeLockCount = 0;
    public bool IsInputLocked => freezeLockCount > 0;
    [Header("---- 스테이지 진행도 ----")]
    private Dictionary<string, int> stageKillCounts = new Dictionary<string, int>();
    private Dictionary<string, int> checkpointKillCounts = new Dictionary<string, int>();
    public void MarkKeyGuideSeen()
    {
        HasSeenKeyGuide = true;
        GameEvents.Stage.RaiseKeyGuideCompleted();
    }

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameEvents.Player.OnDied += GameOver;
            GameEvents.Player.OnCheckpointCreated += HandlePlayerCheckpointCreated;

        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameEvents.Player.OnDied -= GameOver;
        GameEvents.Player.OnCheckpointCreated -= HandlePlayerCheckpointCreated;
    }

    void Update()
    {
        EscCtr();

    }

    public int GetKillCount(string stageGroupId) =>
    stageKillCounts.TryGetValue(stageGroupId, out int count) ? count : 0;

    public void AddKillCount(string stageGroupId, int amount = 1)
    {
        if (string.IsNullOrEmpty(stageGroupId)) return;
        if (!stageKillCounts.ContainsKey(stageGroupId)) stageKillCounts[stageGroupId] = 0;
        stageKillCounts[stageGroupId] += amount;
    }

    public void AcquireFreeze()
    {
        freezeLockCount++;
        Time.timeScale = 0f;
    }

    public void ReleaseFreeze()
    {
        freezeLockCount = Mathf.Max(0, freezeLockCount - 1);
        if (freezeLockCount == 0)
        {
            Time.timeScale = 1f;
        }
    }

    private void HandlePlayerCheckpointCreated(CheckpointSnapshot snapshot)
    {
        lastCheckpointPosition = snapshot.position;
        hasCheckpoint = true;
        savedHealth = snapshot.health;
        savedSkillGauge = snapshot.skillGauge;
        savedCheckpointGauge = snapshot.checkpointGauge;
        savedAccumulatedSouls = snapshot.accumulatedSouls;

        checkpointKillCounts = new Dictionary<string, int>(stageKillCounts); // 킬 진행도도 같이 스냅샷
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsRespawningFromCheckpoint = spawnAtCheckpoint;

        if (!spawnAtCheckpoint || !hasCheckpoint)
        {
            spawnAtCheckpoint = false;
            return;
        }

        stageKillCounts = new Dictionary<string, int>(checkpointKillCounts); // 킬 진행도 복원

        GameEvents.Stage.RaiseCheckpointRestore(new CheckpointSnapshot
        {
            position = lastCheckpointPosition,
            health = savedHealth,
            skillGauge = savedSkillGauge,
            checkpointGauge = savedCheckpointGauge,
            accumulatedSouls = savedAccumulatedSouls
        });

        spawnAtCheckpoint = false;
    }

    public void GameOver()
    {
        ShowGameOverPanel();

        EndPanel.SetActive(true);

    }


    public void IsPause()
    {
        isesc = true;
        PausePanel.SetActive(true);
        PausePanel.transform.localScale = Vector3.zero;
        // 타임스케일 멈추기 전에 애니메이션 실행
        PausePanel.transform.DOScale(Vector3.one, 0.5f)
            .SetUpdate(true)
            .SetEase(Ease.OutBack);
        Time.timeScale = 0.0f;



    }

    public void Resume()
    {
        //SoundMgr.Instance.PlayEffSound("SFX_UI_Button_Click_Settings_2", 0.5f);
        PausePanel.transform.DOScale(Vector3.zero, 0.3f)
       .SetUpdate(true)
       .SetEase(Ease.InBack)
       .OnComplete(() =>
       {
           PausePanel.SetActive(false);
           Time.timeScale = 1f;
           isesc = false;

       });

    }

    void EscCtr()
    {
        if (IsInputLocked) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isesc)
            {
                Resume();
            }
            else
            {
                IsPause();
            }
        }
    }

    public void RegisterSceneRefs(GameObject pausePanel, GameObject endPanel, Button retryBtn, Button lastCheckBtn)
    {

        PausePanel = pausePanel;
        EndPanel = endPanel;
        RetryBtn = retryBtn;
        LastCheckBtn = lastCheckBtn;

        PausePanel.SetActive(false);
        EndPanel.SetActive(false);
    }

    void ShowGameOverPanel()
    {
        EndPanel.transform.localScale = Vector3.zero;
        EndPanel.SetActive(true);

        // 부드럽게 커지며 등장 (0.5초 동안)
        EndPanel.transform.DOScale(Vector3.one, 2f).SetEase(Ease.OutBack);
    }

    public void TitleBack()
    {
        Time.timeScale = 1f;
        //SoundMgr.Instance.PlayEffSound("SFX_UI_Button_Click_Settings_2", 0.5f);
        LastCheckBtn.transform.DOShakePosition(0.3f, 10, 20, 90, false, true)
           .SetUpdate(true)
           .OnComplete(() =>
           {
               SceneManager.LoadScene("TitleScene");
           });
    }

    public void ReGame()
    {
        //SoundMgr.Instance.PlayEffSound("SFX_UI_Button_Click_Settings_2", 0.5f);
        RetryBtn.transform.DOScale(1.2f, 0.1f)
    .SetEase(Ease.OutQuad)
    .SetUpdate(true)
    .OnComplete(() =>
    {
        RetryBtn.transform.DOScale(1.0f, 0.1f).SetEase(Ease.InQuad);
        Time.timeScale = 1f;

        isesc = false;
        spawnAtCheckpoint = false;
        stageKillCounts.Clear();
        SceneManager.LoadScene("GameScene");
    });

    }

    private readonly HashSet<string> seenTutorialIds = new HashSet<string>();

    public bool HasSeenTutorial(string id)
    {
        return !string.IsNullOrEmpty(id) && seenTutorialIds.Contains(id);
    }

    public void MarkTutorialSeen(string id)
    {
        if (!string.IsNullOrEmpty(id)) seenTutorialIds.Add(id);
    }

    public void LoadLastCheckpoint()
    {
        if (!hasCheckpoint)
        {
            // 체크포인트가 없으면 그냥 일반 재시작으로 처리
            ReGame();
            return;
        }

        LastCheckBtn.transform.DOScale(1.2f, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                LastCheckBtn.transform.DOScale(1.0f, 0.1f).SetEase(Ease.InQuad);
                Time.timeScale = 1f;
                isesc = false;
                spawnAtCheckpoint = true;
                SceneManager.LoadScene("GameScene");
            });
    }

}
