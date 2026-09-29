using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EagleEnemy : EnemyBase
{
    private Rigidbody2D rb;

    protected override void Awake()
    {
        base.Awake();
        TryGetComponent(out rb);
        rb.gravityScale = 0f;
    }

    protected override void UpdateBehavior()
    {
        float dist = Vector2.Distance(transform.position, target.position);
        if (dist <= status.detectRange)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            rb.linearVelocity = dir * status.moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // 감지 범위 밖이면 제자리
        }
    }
}