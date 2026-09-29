using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("---- 체력 ----")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private GameObject[] heartIcons;
    [SerializeField] private float invincibilityDuration = 1f;
    private int currentHealth;
    [SerializeField] private bool isInvincible;
    [Header("---- 스킬 게이지 ----")]
    [SerializeField] private int maxSkillGauge = 5;
    [SerializeField] private GameObject[] skillGaugeIcons;
    private int currentSkillGauge;
    public int CurrentSkillGauge => currentSkillGauge;
    [Header("---- 체크포인트 게이지 ----")]
    [SerializeField] private int maxCheckpointGauge = 3;
    [SerializeField] private int soulsRequiredPerPoint = 10;
    [SerializeField] private Image checkpointFillImage;
    [SerializeField] private Text checkpointCountText;
    [SerializeField] private float gaugeFillDuration = 0.4f;
    [SerializeField] private KeyCode createCheckpointKey = KeyCode.E;
    [SerializeField] private ParticleSystem checkpointParticle;
    [SerializeField] private float checkpointHoldTime = 1f;
    private int currentCheckpointGauge;
    private float checkpointHoldTimer;
    private int accumulatedSouls;
    private Queue<System.Action> gaugeAnimQueue = new Queue<System.Action>();
    private bool isAnimatingGauge;
    private GameObject currentCheckpointEffect;

    [Header("---- 피격 이펙트 ----")]
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private Color flashColor = Color.red;
    private MaterialPropertyBlock flashBlock;

    private Animator animator;
    private PlayerCtrl playerCtrl;
    private bool restoredFromCheckpoint;
    [SerializeField] private string deadLayerName = "PlayerDead";

    private void Awake()
    {
        currentSkillGauge = 0;
        currentCheckpointGauge = 0;
        currentHealth = maxHealth;

        TryGetComponent(out animator);
        TryGetComponent(out playerCtrl);
        flashBlock = new MaterialPropertyBlock();
    }

    private void OnEnable() => GameEvents.Stage.OnCheckpointRestore += HandleCheckpointRestore;
    private void OnDisable() => GameEvents.Stage.OnCheckpointRestore -= HandleCheckpointRestore;

    private void Start()
    {
        UpdateHeartsUI();
        UpdateSkillGaugeUI();
        if (!restoredFromCheckpoint)   // 복원된 적 없을 때만 기본값 세팅
        {
            checkpointFillImage.fillAmount = 0f;
            checkpointCountText.text = "0";
        }

    }

    private void Update()
    {
        if (GameManager.inst != null && GameManager.inst.IsInputLocked) return;

        if (Input.GetKey(createCheckpointKey))
        {
            if (CanCreateCheckpoint())
            {
                checkpointHoldTimer += Time.deltaTime;

                if (checkpointHoldTimer >= checkpointHoldTime)
                {
                    CreateCheckpoint();
                    checkpointHoldTimer = -1f; // 생성 후 키를 뗄 때까지 재발동 방지
                }
            }
            else
            {
                checkpointHoldTimer = 0f; // 조건 불충족(게이지 없음/공중)이면 리셋
            }
        }
        else
        {
            checkpointHoldTimer = 0f; // 키를 떼면 즉시 리셋
        }
    }

    private void HandleCheckpointRestore(CheckpointSnapshot snap)
    {
        restoredFromCheckpoint = true;
        transform.position = snap.position;   // GameManager가 직접 위치를 옮기지 않아도 됨
        ApplyCheckpointState(snap.health, snap.skillGauge, snap.checkpointGauge, snap.accumulatedSouls);
    }

    public void SetInvincible(bool value) //바쉬 중 무적처리하기
    {
        isInvincible = value;
    }

    private bool CanCreateCheckpoint()
    {
        return currentCheckpointGauge > 0 && playerCtrl != null && playerCtrl.IsGrounded;
    }


    private void CreateCheckpoint()
    {
        currentCheckpointGauge--;
        checkpointCountText.text = currentCheckpointGauge.ToString();

        GameEvents.Player.RaiseCheckpointCreated(new CheckpointSnapshot
        {
            position = transform.position,
            health = currentHealth,
            skillGauge = currentSkillGauge,
            checkpointGauge = currentCheckpointGauge,
            accumulatedSouls = accumulatedSouls
        });

        if (currentCheckpointEffect != null)
            Destroy(currentCheckpointEffect);
        currentCheckpointEffect = Instantiate(checkpointParticle, transform.position, Quaternion.identity).gameObject;
    }

    public void FillCheckpointGauge(int amount)
    {
        if (currentCheckpointGauge >= maxCheckpointGauge) return;

        int previousGauge = currentCheckpointGauge;
        int totalSouls = accumulatedSouls + amount;
        int pointsGained = 0;

        while (totalSouls >= soulsRequiredPerPoint && currentCheckpointGauge + pointsGained < maxCheckpointGauge)
        {
            totalSouls -= soulsRequiredPerPoint;
            pointsGained++;
        }

        currentCheckpointGauge += pointsGained;
        accumulatedSouls = currentCheckpointGauge >= maxCheckpointGauge ? 0 : totalSouls;

        bool showTutorial = previousGauge == 0 && pointsGained > 0;
        int finalAccumulated = accumulatedSouls; // 클로저용 스냅샷

        gaugeAnimQueue.Enqueue(() => AnimateCheckpointGaugeStep(previousGauge, pointsGained, finalAccumulated, showTutorial));

        if (!isAnimatingGauge)
            ProcessGaugeQueue();
    }

    private void ProcessGaugeQueue()
    {
        if (gaugeAnimQueue.Count == 0)
        {
            isAnimatingGauge = false;
            return;
        }

        isAnimatingGauge = true;
        gaugeAnimQueue.Dequeue().Invoke();
    }


    private void AnimateCheckpointGaugeStep(int previousGauge, int pointsGained, int finalAccumulated, bool showTutorial)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true); // 매번 새 시퀀스 (Append 아님)

        for (int i = 1; i <= pointsGained; i++)
        {
            int displayValue = previousGauge + i;
            seq.Append(checkpointFillImage.DOFillAmount(1f, gaugeFillDuration).SetEase(Ease.OutQuad).SetUpdate(true));
            seq.AppendCallback(() => checkpointCountText.text = displayValue.ToString());
            seq.AppendInterval(0.1f);
            seq.AppendCallback(() => checkpointFillImage.fillAmount = 0f);
        }

        float finalFill = (float)finalAccumulated / soulsRequiredPerPoint;
        seq.Append(checkpointFillImage.DOFillAmount(finalFill, gaugeFillDuration).SetEase(Ease.OutQuad).SetUpdate(true));

        seq.OnComplete(() =>
        {
            if (showTutorial)
                TutorialMgr.Instance?.RequestShow(TutorialIds.SaveGauge);

            ProcessGaugeQueue(); // 대기 중인 다음 애니메이션 이어서 실행
        });
    }

    public void ApplyCheckpointState(int health, int skillGauge, int checkpointGauge, int accumulatedSoulsValue)
    {
        currentHealth = health;
        currentSkillGauge = skillGauge;
        currentCheckpointGauge = checkpointGauge;
        accumulatedSouls = accumulatedSoulsValue;

        UpdateHeartsUI();
        UpdateSkillGaugeUI();
        checkpointFillImage.fillAmount = (float)accumulatedSouls / soulsRequiredPerPoint;
        checkpointCountText.text = currentCheckpointGauge.ToString();
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || playerCtrl.IsDie) 
            return;
        TutorialMgr.Instance?.RequestShow(TutorialIds.Bash);
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        UpdateHeartsUI();
        animator.SetTrigger("Hit");
        StartCoroutine(FlashRoutine());

        if (currentHealth <= 0)
        {
            Die();

        }
        else
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        foreach (var r in flashRenderers)
        {
            r.GetPropertyBlock(flashBlock);
            flashBlock.SetColor("_BaseColor", flashColor);
            r.SetPropertyBlock(flashBlock);
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (var r in flashRenderers)
        {
            r.GetPropertyBlock(flashBlock);
            flashBlock.SetColor("_BaseColor", Color.white);
            r.SetPropertyBlock(flashBlock);
        }
    }

    private void UpdateHeartsUI()
    {
        for (int i = 0; i < heartIcons.Length; i++)
        {
            heartIcons[i].SetActive(i < currentHealth);
        }
    }
    private void UpdateSkillGaugeUI()
    {
        for (int i = 0; i < skillGaugeIcons.Length; i++)
        {
            skillGaugeIcons[i].SetActive(i < currentSkillGauge);
        }
    }


    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHeartsUI();
    }

    public void FillSkillGauge(int amount)
    {
        currentSkillGauge = Mathf.Min(currentSkillGauge + amount, maxSkillGauge);
        UpdateSkillGaugeUI();

        TutorialMgr.Instance?.RequestShow(TutorialIds.SkillGauge);
    }

    // 차지 공격 시 게이지 소모. 성공하면 true, 게이지 부족하면 false
    public bool UseSkillGauge(int amount)
    {
        if (currentSkillGauge < amount) return false;

        currentSkillGauge -= amount;
        UpdateSkillGaugeUI();
        return true;
    }


    private void Die()
    {
        playerCtrl.Kill();
        animator.SetBool("IsDead", true);
        animator.SetTrigger("Die");
        if (playerCtrl != null) playerCtrl.enabled = false; //조작 정지
        gameObject.layer = LayerMask.NameToLayer(deadLayerName);
        GameEvents.Player.RaiseDied();
    }
}