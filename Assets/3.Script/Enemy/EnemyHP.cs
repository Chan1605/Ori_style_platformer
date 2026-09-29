using UnityEngine;
using UnityEngine.UI;

public class EnemyHP : MonoBehaviour
{
    [SerializeField] private Image hpBarFill;
    private int maxHp;
    private EnemyBase owner;

    public void Setup(EnemyBase enemy, int max)
    {
        owner = enemy;
        maxHp = max;
        UpdateHPBar();
    }

    public void UpdateHPBar()
    {
        if (hpBarFill == null || owner == null || maxHp <= 0) return;
        hpBarFill.fillAmount = (float)owner.CurrentHp / maxHp;
    }
}