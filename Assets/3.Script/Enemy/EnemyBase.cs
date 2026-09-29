using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [SerializeField] protected EnemyStatus status;
    [SerializeField] protected EnemyHP hpBarComponent;
    [Header("---- 접촉 데미지 ----")]
    [SerializeField] protected float contactDamageCooldown = 0.5f;
    [SerializeField] protected bool applyKnockbackOnContact = true;
    [SerializeField] protected float knockbackMultiplier = 1f;

    protected new SpriteRenderer renderer;
    protected int currentHp;
    public int Damage => status.damage;
    protected Transform target;
    protected PlayerCtrl targetPlayer;

    private float lastContactDamageTime = -100f;
    [SerializeField] protected string stageGroupId; 
    public string StageGroupId => stageGroupId;

    public static event System.Action<EnemyBase> OnAnyEnemyDeath; // 전역 사망 알림


    protected virtual void Awake()
    {
        currentHp = status.maxHp;
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        TryGetComponent(out renderer);
        target?.TryGetComponent(out targetPlayer);
    }

    protected virtual void Start()
    {
        if (hpBarComponent != null) hpBarComponent.Setup(this, status.maxHp);
    }

    protected virtual void Update()
    {
        if (targetPlayer != null && targetPlayer.IsDie) return;
        UpdateBehavior();
    }

    protected abstract void UpdateBehavior();

    public void SetStageGroupId(string id)
    {
        if (!string.IsNullOrEmpty(id)) stageGroupId = id;
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < lastContactDamageTime + contactDamageCooldown) return;

        lastContactDamageTime = Time.time;

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(Damage);
        }

        if (applyKnockbackOnContact && other.TryGetComponent(out PlayerCtrl player))
        {
            Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
            player.ApplyKnockback(dir, knockbackMultiplier);
        }
    }

    public int CurrentHp => currentHp;
    public int MaxHp => status.maxHp;

    private IEnumerator Hitanimation_co()
    {
        renderer.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        renderer.color = Color.white;
    }

    public void SetStatus(EnemyStatus newStatus)
    {
        if (newStatus == null) return;
        status = newStatus;
        currentHp = status.maxHp;
        if (hpBarComponent != null) hpBarComponent.Setup(this, status.maxHp);
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        hpBarComponent?.UpdateHPBar();
        StopCoroutine("Hitanimation_co");
        StartCoroutine("Hitanimation_co");
        if (currentHp <= 0) Die();
    }

    public event System.Action<EnemyBase> OnDeath;

    protected virtual void Die()
    {
        SpawnDrops();
        OnDeath?.Invoke(this);
        OnAnyEnemyDeath?.Invoke(this); 
        gameObject.SetActive(false);
    }

    public virtual void ResetEnemy()
    {
        currentHp = status.maxHp;
        renderer.color = Color.white;
        if (hpBarComponent != null) hpBarComponent.Setup(this, status.maxHp);
    }

    protected virtual void SpawnDrops()
    {
        if (status.drops == null) return;
        foreach (var drop in status.drops)
        {
            if (drop.itemPrefabs == null || drop.itemPrefabs.Length == 0) continue;
            if (Random.value > drop.dropChance) continue;
            
            int totalAmount = Random.Range(drop.minAmount, drop.maxAmount + 1);
           // GameObject dropPrefab = drop.itemPrefabs[Random.Range(0, drop.itemPrefabs.Length)]; //한 가지 아이템만 뽑을 때

            for (int i = 0; i < totalAmount; i++)
            {
                GameObject dropPrefab = drop.itemPrefabs[Random.Range(0, drop.itemPrefabs.Length)];
                GameObject dropObj = Instantiate(dropPrefab, transform.position + Vector3.up, Quaternion.identity);
                if (dropObj.TryGetComponent(out ItemPickup pickup))
                {
                    //int value = Random.Range(drop.minAmount, drop.maxAmount + 1);
                    //float t = Mathf.InverseLerp(drop.minAmount, drop.maxAmount, value);
                    //float scale = Mathf.Lerp(0.8f, 1.5f, t);
                    //pickup.SetScale(scale);

                    //pickup.SetAmount(value);
                    pickup.PopLaunch();                   
                }
            }
        }
    }
}