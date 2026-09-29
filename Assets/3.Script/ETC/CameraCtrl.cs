using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCtrl : MonoBehaviour
{
    [Header("---- 플레이어 추적 ----")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;

    [Header("---- 속도조절 ----")]
    [SerializeField] private float xSpeed = 5f;
    [SerializeField] private float ySpeed = 2f;

    [Header("---- Y값 제한 ----")]
    [SerializeField] private float yDeadZone = 1.0f;

    [Header("---- 시야 미리보기 ----")]
    [SerializeField] private float lookAheadAmount = 2f;
    [SerializeField] private float lookAheadSpeed = 3f;
    private float currentLookAhead = 0f;
    private float targetLookAhead = 0f;

    [Header("---- 맵 제한 ----")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Transform bgRoot;
    [SerializeField] private float bottomPaddingExtra = 2f;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    [Header("---- 위/아래 시야 미리보기 ----")]
    [SerializeField] private float peekAmount = 3f;   // 최대로 밀리는 정도
    [SerializeField] private float peekSpeed = 4f;    // 밀리고 돌아오는 속도
    [SerializeField] private float peekHoldDelay = 0.25f; // 이만큼 눌러야 발동
    private float peekOffsetY;
    private float peekTargetY;
    [SerializeField] private PlayerCtrl player; // 상태 확인
    private float peekHoldTimer;

    [SerializeField] private Camera cam;
    [SerializeField] private Vector3 shakeOffset;

    private float lastTargetX;

    private void Start()
    {
        if (cam == null)
        {
            TryGetComponent(out cam);
        }
        lastTargetX = target.position.x;

        CalculateBoundsFromBG();
    }

    public void Shake(float duration = 0.12f, float magnitude = 0.15f)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }
    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            shakeOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * magnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }

    private void CalculateBoundsFromBG()
    {
        if (bgRoot == null)
        {
            useBounds = false;
            return;
        }

        Renderer[] childRenderers = bgRoot.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length == 0)
        {
            useBounds = false;
            return;
        }

        Bounds combined = childRenderers[0].bounds;
        for (int i = 1; i < childRenderers.Length; i++)
        {
            combined.Encapsulate(childRenderers[i].bounds);
        }

        minBounds = new Vector2(combined.min.x, combined.min.y);
        maxBounds = new Vector2(combined.max.x, combined.max.y);
        minBounds.y += bottomPaddingExtra;
    }

    private void Update()
    {
        bool playerBusy = player != null && (player.IsClimbing || player.IsDashing || player.IsBashing || !player.IsGrounded);

        bool holdingW = !playerBusy && Input.GetKey(KeyCode.W);
        bool holdingS = !playerBusy && Input.GetKey(KeyCode.S);

        if (holdingW || holdingS)
        {
            peekHoldTimer += Time.deltaTime;
        }
        else
        {
            peekHoldTimer = 0f;
        }

        // 충분히 눌렀을 때만 목표값을 실제로 갱신, 아니면 항상 0(원위치)으로
        peekTargetY = (peekHoldTimer >= peekHoldDelay) ? (holdingW ? peekAmount : (holdingS ? -peekAmount : 0f)) : 0f;
    }

    private void FixedUpdate()
    {
        float xDelta = target.position.x - lastTargetX;
        if (xDelta > 0.01f)
        {
            targetLookAhead = lookAheadAmount;
        }
        else if (xDelta < -0.01f)
        {
            targetLookAhead = -lookAheadAmount;
        }
        lastTargetX = target.position.x;
        
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, Time.fixedDeltaTime * lookAheadSpeed);


        peekOffsetY = Mathf.Lerp(peekOffsetY, peekTargetY, Time.fixedDeltaTime * peekSpeed);

        float newX = Mathf.Lerp(transform.position.x, target.position.x + offset.x + currentLookAhead, Time.fixedDeltaTime * xSpeed);

        float desiredY = target.position.y + offset.y + peekOffsetY; // peekOffsetY를 목표값에 더함
        float newY = transform.position.y;
        float yDiff = desiredY - transform.position.y;
        if (Mathf.Abs(yDiff) > yDeadZone)
        {
            newY = Mathf.Lerp(transform.position.y, desiredY, Time.fixedDeltaTime * ySpeed);
        }

        Vector3 newPos = new Vector3(newX, newY, offset.z);

        if (useBounds && cam != null)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            float clampMinX = minBounds.x + halfWidth;
            float clampMaxX = maxBounds.x - halfWidth;
            float clampMinY = minBounds.y + halfHeight;
            float clampMaxY = maxBounds.y - halfHeight;

            if (clampMinX <= clampMaxX)
            {
                newPos.x = Mathf.Clamp(newPos.x, clampMinX, clampMaxX);
            }
            if (clampMinY <= clampMaxY)
            {
                newPos.y = Mathf.Clamp(newPos.y, clampMinY, clampMaxY);
            }
        }
        transform.position = newPos + shakeOffset;
    }
}