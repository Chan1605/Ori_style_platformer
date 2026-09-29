using UnityEngine;

public class FrogEnemy : EnemyBase, IRangedEnemy
{
    [Header("---- 개구리 전용 ----")]
    private BulletPool pool; 
    [SerializeField] private Transform firePoint;
     private Transform aimPointOverride; //플레이어 hit박스
    private float lastAttackTime = -100f;

    protected override void Awake()
    {
        base.Awake();
        if (target != null)
        {
            Transform aim = target.Find("HitPoint");
            if (aim != null) aimPointOverride = aim;
        }
    }

    protected override void Start()
    {
        base.Start();
        if (pool == null) //임시
        {
            GameObject poolObj = GameObject.Find("EnemyBulletPool"); // 씬에서 이름으로 직접 찾기
            if (poolObj != null) poolObj.TryGetComponent(out pool);
        }
    }



    private Vector3 GetAimPosition()
    {
        return aimPointOverride != null ? aimPointOverride.position : target.position;
    }
    protected override void UpdateBehavior()
    {
        if (pool == null)
            return;

        FaceTarget();

        Vector3 aimPos = GetAimPosition();
        float dist = Vector2.Distance(transform.position, aimPos);
        if (dist <= status.attackRange && Time.time - lastAttackTime > status.attackCooldown)
        {
            Vector2 dir = (aimPos - firePoint.position).normalized;
            pool.Get(firePoint.position, dir);
            lastAttackTime = Time.time;
        }
    }

    private void FaceTarget()
    {
        if (target == null) return;

        float dir = target.position.x > transform.position.x ? 1f : -1f;
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;
    }

    public void SetBulletPool(BulletPool bulletPool) 
    {
        pool = bulletPool;
    }

}