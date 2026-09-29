using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumperEnemy : EnemyBase
{
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("---- 패트롤 ----")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Transform ledgeCheck;
    [SerializeField] private float patrolSpeedMultiplier = 0.5f;

    [Header("---- 추격 ----")]
    [SerializeField] private float stoppingDistance = 2f; // 이 거리 안이면 수평 이동 멈춤

    private Rigidbody2D rb;
    private float lastJumpTime = -100f;
    private int patrolDirection = 1; // 1: B쪽, -1: A쪽

    protected override void Awake()
    {
        base.Awake();
        TryGetComponent(out rb);
    }

    protected override void UpdateBehavior()
    {
        float dist = Vector2.Distance(transform.position, target.position);
        bool grounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        if (dist <= status.detectRange)
        {
            ChaseTarget(grounded);
        }
        else
        {
            Patrol(grounded);
        }
    }

    private void ChaseTarget(bool grounded)
    {
        float dir = target.position.x > transform.position.x ? 1f : -1f;
        SetFacing(dir);

        float dist = Vector2.Distance(transform.position, target.position);

        if (dist > stoppingDistance)
        {
            rb.linearVelocity = new Vector2(dir * status.moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // 정지 거리 도달 → 수평 이동만 멈춤
        }

        if (grounded && Time.time - lastJumpTime > status.attackCooldown)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            lastJumpTime = Time.time;
        }
    }

    private void Patrol(bool grounded)
    {
        Transform targetPoint = patrolDirection > 0 ? patrolPointB : patrolPointA;
        bool reachedPoint = Mathf.Abs(transform.position.x - targetPoint.position.x) < 0.1f;

        bool hitWall = wallCheck != null &&
            Physics2D.Raycast(wallCheck.position, Vector2.right * patrolDirection, wallCheckDistance, wallLayer);

        bool noGroundAhead = ledgeCheck != null &&
            !Physics2D.OverlapCircle(ledgeCheck.position, 0.1f, groundLayer);

        if (reachedPoint || hitWall || noGroundAhead)
        {
            patrolDirection *= -1;
        }

        SetFacing(patrolDirection);
        rb.linearVelocity = new Vector2(patrolDirection * status.moveSpeed * patrolSpeedMultiplier, rb.linearVelocity.y);

        if (grounded && Time.time - lastJumpTime > status.attackCooldown)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            lastJumpTime = Time.time;
        }
    }

    private void SetFacing(float dir)
    {
        if (Mathf.Abs(dir) < 0.01f) return;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dir);
        transform.localScale = scale;
    }
}