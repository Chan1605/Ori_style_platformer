using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnDelay = 5f;
    [SerializeField] private BulletPool enemyBulletPool;
    [SerializeField] private EnemyStatus overrideStatus;
    [SerializeField] private string overrideStageGroupId;
    [SerializeField] private bool autoStartOnAwake = true; // 스테이지1처럼 시작하자마자 스폰할지

    private Dictionary<GameObject, Transform> enemyToSpawnPoint = new Dictionary<GameObject, Transform>();
    private bool isActive;

    private void Start()
    {
        if (autoStartOnAwake) Activate();
    }

    public void Activate()
    {
        if (isActive) return;
        isActive = true;

        foreach (var point in spawnPoints)
        {
            SpawnAt(point);
        }
    }

    public void Deactivate()
    {
        isActive = false;
        StopAllCoroutines(); // 대기 중인 리스폰 코루틴 전부 취소

        foreach (var kvp in enemyToSpawnPoint)
        {
            if (kvp.Key != null)
                kvp.Key.SetActive(false); // 현재 살아있는 개체도 즉시 비활성화
        }
    }

    private void SpawnAt(Transform point)
    {
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject enemy = Instantiate(prefab, point.position, Quaternion.identity, transform);

        if (enemy.TryGetComponent<EnemyBase>(out var enemyBase))
        {
            if (overrideStatus != null)
                enemyBase.SetStatus(overrideStatus);
            if (!string.IsNullOrEmpty(overrideStageGroupId))
                enemyBase.SetStageGroupId(overrideStageGroupId);

            enemyBase.OnDeath += HandleEnemyDeath;
        }

        if (enemy.TryGetComponent<IRangedEnemy>(out var ranged))
        {
            ranged.SetBulletPool(enemyBulletPool);
        }

        enemyToSpawnPoint[enemy] = point;
    }

    private void HandleEnemyDeath(EnemyBase enemy)
    {
        if (!isActive) return; // 비활성화된 그룹이면 리스폰 안 함
        StartCoroutine(RespawnRoutine(enemy));
    }

    private IEnumerator RespawnRoutine(EnemyBase enemy)
    {
        yield return new WaitForSeconds(respawnDelay);
        if (!isActive) yield break; // 대기 중 그룹이 꺼지면 취소

        if (enemyToSpawnPoint.TryGetValue(enemy.gameObject, out Transform point))
        {
            enemy.transform.position = point.position;
            enemy.ResetEnemy();
            enemy.gameObject.SetActive(true);
        }
    }
}