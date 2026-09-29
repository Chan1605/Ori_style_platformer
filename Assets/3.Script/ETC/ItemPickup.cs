using UnityEngine;

public enum ItemType { Soul, Heal, CheckpointKey }

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemType type;
    [SerializeField] private int amount = 1;
    [SerializeField] private float magnetRange = 3f;
    [SerializeField] private float magnetSpeed = 8f;
    [SerializeField] private float lifeTime = 10f;
    [Header("---- 연출 ----")]
    [SerializeField] private float popUpForce = 4f;
    [SerializeField] private float popScatterRange = 2f;// 좌우로 흩어지는 정도

    private Transform target;
    private Rigidbody2D rb;
    private bool isMagnetized;

    private void Awake()
    {
        TryGetComponent(out rb);
    }
    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        Destroy(gameObject, lifeTime);
    }

    public void SetAmount(int value) => amount = value;

    public void SetScale(float scale)
    {
        transform.localScale = Vector3.one * scale;
        // if (TryGetComponent(out SpriteRenderer sr)) 색상 변경 시
        //     sr.color = Color.Lerp(baseColor, highlightColor, someT);
    }

    public void PopLaunch()
    {
        float randomX = Random.Range(-popScatterRange, popScatterRange);
        rb.linearVelocity = new Vector2(randomX, popUpForce);
    }
    private void Update()
    {
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist <= magnetRange) isMagnetized = true;

        if (isMagnetized)
        {
            if (TryGetComponent(out Collider2D col)) col.isTrigger = true;  //자석효과면 지형을 뚫고 오도록
            rb.gravityScale = 0f;
            transform.position = Vector2.MoveTowards(transform.position, target.position, magnetSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent(out PlayerHealth playerHealth)) return;

        switch (type)
        {
            case ItemType.Soul:
                playerHealth.FillSkillGauge(amount);
                break;
            case ItemType.Heal:
                playerHealth.Heal(amount);
                break;
            case ItemType.CheckpointKey:
                playerHealth.FillCheckpointGauge(amount);

                break;
        }

        Destroy(gameObject);
    }
}