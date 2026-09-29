using UnityEngine;

public class SeinCtrl : MonoBehaviour
{
    [Header("----- 플레이어 추적 -----")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 followOffset = new Vector2(0.5f, 1f);
    [SerializeField] private float followSmoothTime = 0.15f;
    private Vector3 followVelocity;
    private float lastTargetX;
    private float facingSign = -1f;

    [Header("---- 대기 연출 ----")]
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatFrequency = 2f;

    [Header("---- 스킬 게이지 ----")]
    [SerializeField] private PlayerHealth playerHealth; // 게이지 확인/소모용
    [SerializeField] private int chargeSkillCost = 1;

    [Header("---- 공격 ----")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerCtrl playerCtrl;
    [SerializeField] private BulletPool pool;
    [Header("---- 차지공격 ----")]
    [SerializeField] private ParticleSystem chargeParticle;
    [SerializeField] private ParticleSystem explosionParticle;
    [SerializeField] private float holdChargeTime = 2f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int explosionDamage = 3;
    [SerializeField] private float chargeVfxDelay = 0.15f;
    private bool hasStartedChargeVfx;
    private float chargeTimer;
    private bool isCharging;
    [Header("방향 각도 조절")]
    [SerializeField] private float[] spreadAngles = new float[] { -15f, 0f, 15f }; 
    private int spreadIndex = 0;

    private float lastAttackTime = -100f;
    private CameraCtrl cam;

    private void Awake()
    {
        if (cam == null)
        {
            cam = FindAnyObjectByType<CameraCtrl>();
        }

        if (playerHealth == null && playerCtrl != null)
        {
            playerCtrl.TryGetComponent(out playerHealth);
        }
    }

    private void Start()
    {
        if (target == null)
        {
            enabled = false;
            return;
        }

        lastTargetX = target.position.x;
    }

    private void Update()
    {
        if (playerCtrl.IsDie)
            return;
        FollowTarget();

        if (Input.GetMouseButtonDown(0))
        {
            if (playerHealth.CurrentSkillGauge < chargeSkillCost)
            {
                // 게이지 부족: 차지 없이 즉시 일반 공격 처리
                TryFire();
                return;
            }
            chargeTimer = 0f;
            isCharging = true;
            hasStartedChargeVfx = false; 
        }

        if (isCharging && Input.GetMouseButton(0))
        {
            chargeTimer += Time.deltaTime;

            if (!hasStartedChargeVfx && chargeTimer >= chargeVfxDelay)
            {
                chargeParticle.Play();
                hasStartedChargeVfx = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isCharging = false;

            if (hasStartedChargeVfx)
            {
                chargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (chargeTimer >= holdChargeTime && playerHealth.UseSkillGauge(chargeSkillCost))
            {
                FireChargeExplosion();
            }
            else
            {
                TryFire();
            }

            chargeTimer = 0f;
        }
    }

    private void TryFire()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        Transform nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            FireAt(nearestEnemy); // 적이 있으면 정조준
        }
        else
        {
            FireSpread(); // 적이 없으면 순환 각도로 한 발
        }

        lastAttackTime = Time.time;
    }

    private void FireChargeExplosion()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        explosionParticle.transform.position = spawnPos;
        explosionParticle.Play();

        Collider2D[] hits = Physics2D.OverlapCircleAll(spawnPos, explosionRadius, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(explosionDamage);
            }
        }
        cam.Shake(0.25f, 0.35f);
    }

    private void FireAt(Transform enemy)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = (enemy.position - spawnPos).normalized;

        pool.Get(spawnPos, dir);
    }

    private void FireSpread()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        // 플레이어가 오른쪽을 보면 0도 기준, 왼쪽을 보면 180도 기준으로 뒤집음
        float baseAngle = playerCtrl.FacingDirection > 0f ? 0f : 180f;
        float angle = baseAngle + spreadAngles[spreadIndex];
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        pool.Get(spawnPos, dir);
        // 다음 클릭엔 배열의 다음 각도로 -> 끝까지 가면 처음으로 순환
        spreadIndex = (spreadIndex + 1) % spreadAngles.Length;
    }

    private void FollowTarget()
    {
        float xDelta = target.position.x - lastTargetX;
        if (xDelta > 0.01f)
        {
            facingSign = -1f;
        }
        else if (xDelta < -0.01f)
        {
            facingSign = 1f;
        }
        lastTargetX = target.position.x;

        float floatY = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        float offsetX = Mathf.Abs(followOffset.x) * facingSign;

        Vector3 desiredPos = target.position + new Vector3(offsetX, followOffset.y, 0f);
        desiredPos.y += floatY;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref followVelocity, followSmoothTime);
    }


    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        if (hits.Length == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            float d = Vector2.Distance(transform.position, hits[i].transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = hits[i].transform;
            }
        }
        return nearest;
    }


    private void OnDrawGizmosSelected() //범위 에디터에서 체크
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}