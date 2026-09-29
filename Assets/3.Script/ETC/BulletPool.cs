using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int prewarmCount = 20; // 미리 만들어둘 총알 개수

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, transform);
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }

    public GameObject Get(Vector3 position, Vector2 direction)
    {
        GameObject bullet;
        if (pool.Count > 0)
        {
            bullet = pool.Dequeue();
        }
        else
        {
            bullet = Instantiate(bulletPrefab, transform);
        }

        bullet.transform.position = position;
        bullet.SetActive(true);

        if (bullet.TryGetComponent(out BulletBase bulletScript))
        {
            bulletScript.Init(this, direction);
        }

        return bullet;
    }

    public void Return(GameObject bullet)
    {
        bullet.SetActive(false);
        pool.Enqueue(bullet);
    }
}