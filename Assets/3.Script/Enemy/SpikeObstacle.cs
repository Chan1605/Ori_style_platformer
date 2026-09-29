using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeObstacle : MonoBehaviour
{
    [SerializeField] private int damage = 10;          
    [SerializeField] private float damageCooldown = 0.5f;     

    private Dictionary<IDamageable, float> damagedTargets = new Dictionary<IDamageable, float>();

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 충돌한 대상이 데미지를 입을 수 있는지 확인
        if (collision.TryGetComponent(out IDamageable damageable))
        {
            // 최근 데미지를 입은 적이 없다면 즉시 데미지 처리
            if (!damagedTargets.ContainsKey(damageable))
            {
                ApplyDamage(damageable);
            }
            // 쿨타임이 지났는지 확인 후 반복 데미지 처리
            else if (Time.time >= damagedTargets[damageable])
            {
                ApplyDamage(damageable);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어가 가시에서 벗어나면 추적 목록에서 제거
        if (collision.TryGetComponent(out IDamageable damageable))
        {
            if (damagedTargets.ContainsKey(damageable))
            {
                damagedTargets.Remove(damageable);
            }
        }
    }

    private void ApplyDamage(IDamageable target)
    {
        
        target.TakeDamage(damage);
        // 다음 데미지 가능 시간 갱신
        damagedTargets[target] = Time.time + damageCooldown;
    }
}