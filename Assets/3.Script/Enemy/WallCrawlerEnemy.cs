using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCrawlerEnemy : EnemyBase
{
    [SerializeField] private float patrolRange = 3f;
    private Vector3 startPos;
    private float direction = 1f;

    protected override void Awake()
    {
        base.Awake();
        startPos = transform.position;
    }

    protected override void UpdateBehavior()
    {
        transform.Translate(Vector2.up * direction * status.moveSpeed * Time.deltaTime); // 벽이 세로라고 가정
        if (Mathf.Abs(transform.position.y - startPos.y) > patrolRange)
        {
            direction *= -1f;
        }
    }
}