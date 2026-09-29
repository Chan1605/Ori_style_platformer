using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public GameObject[] itemPrefabs;
    [Range(0f, 1f)] public float dropChance = 1f;
    public int minAmount = 1;
    public int maxAmount = 1;
}

[CreateAssetMenu(fileName = "EnemyStatus", menuName = "ScriptableObjects/EnemyStatus")]
public class EnemyStatus : ScriptableObject
{
    public DropEntry[] drops;

    public int maxHp = 3;
    public float moveSpeed = 3f;
    public int damage = 1;
    public float detectRange = 6f;
    public float attackRange = 4f;
    public float attackCooldown = 1.5f;


}