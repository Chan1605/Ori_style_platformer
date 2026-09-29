using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BulletBase : MonoBehaviour
{
    [Header("---- 속도 ----")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float lifeTime = 3f;

    [Header("---- 피해량 ----")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask hitLayer;

    [Header("---- 이펙트 ----")]
    [SerializeField] private ParticleSystem projectilePS; 
    [SerializeField] private ParticleSystem hitPS;
    [Header("---- 넉백 (선택사항) ----")]
    [SerializeField] private bool applyKnockback = false;
    [SerializeField] private float knockbackMultiplier = 1f;


    private BulletPool pool;
    private float spawnTime;
    private bool collided;
    private CameraCtrl Camera;

    private void Awake()
    {
        if (Camera == null)
        {
            Camera = FindAnyObjectByType<CameraCtrl>();
        }
        if (TryGetComponent(out Rigidbody2D rb)) rb.bodyType = RigidbodyType2D.Kinematic;
        if (TryGetComponent(out Collider2D col)) col.isTrigger = true;
    }

    public void Init(BulletPool sourcePool, Vector2 direction)
    {
        pool = sourcePool;
        spawnTime = Time.time;
        collided = false;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 재사용 시 파티클 상태 초기화 -> 이전 발사 흔적이 안 남게
        if (projectilePS != null) { projectilePS.Clear(true); projectilePS.Play(true); }

        if (GetComponent<Collider2D>() is Collider2D col2) col2.enabled = true;

    }

    private void Update()
    {
        if (collided) return; // 맞은 뒤에는 더 이상 날아가지 않음

        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime, Space.Self);

        if (Time.time - spawnTime > lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collided) return;
        bool isHitLayer = ((1 << other.gameObject.layer) & hitLayer) != 0;
        if (!isHitLayer) return;
        collided = true;

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(damage);
            Camera.Shake(0.1f,0.15f);
        }

        if (applyKnockback && other.TryGetComponent(out PlayerCtrl player))
        {
            Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
            player.ApplyKnockback(dir, knockbackMultiplier);
        }

        if (TryGetComponent(out Collider2D col)) col.enabled = false;
        if (projectilePS != null) projectilePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float delay = 0.3f;
        if (hitPS != null)
        {
            hitPS.Clear(true);
            hitPS.Play(true);
            delay = Mathf.Max(hitPS.main.duration, 0.05f);
        }
        StartCoroutine(ReturnToPoolAfterHit(delay));
    }

    private IEnumerator ReturnToPoolAfterHit(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    private void OnBecameInvisible()
    {
        if (!collided) ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (pool != null) pool.Return(gameObject);
    }
}