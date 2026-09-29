using UnityEngine;

public class CurrencyMgr : MonoBehaviour
{
    public static CurrencyMgr Instance;
    private int soulCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddSoul(int amount)
    {
        soulCount += amount;
        // TODO: UI 갱신(소울 카운트 텍스트)이 생기면 여기서 이벤트/직접 호출
    }

    public int GetSoulCount() => soulCount;
}