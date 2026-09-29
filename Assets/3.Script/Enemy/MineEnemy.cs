using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineEnemy : EnemyBase
{
    [SerializeField] private float explodeDelay = 0.5f;
    [SerializeField] private float explodeRadius = 2f;
    [SerializeField] private LayerMask playerLayer;
    private bool triggered;

    protected override void UpdateBehavior()
    {
        if (triggered) return;
        float dist = Vector2.Distance(transform.position, target.position);
        if (dist <= status.detectRange)
        {
            triggered = true;
            StartCoroutine(ExplodeRoutine());
        }
    }

    private IEnumerator ExplodeRoutine()
    {
        yield return new WaitForSeconds(explodeDelay);

        Collider2D hit = Physics2D.OverlapCircle(transform.position, explodeRadius, playerLayer);
        if (hit != null && hit.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(status.damage);
        }
        Destroy(gameObject); // 폭발 이펙트는 여기에 나중에 추가
    }
}