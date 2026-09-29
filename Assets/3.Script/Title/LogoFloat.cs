using UnityEngine;

public class LogoFloat : MonoBehaviour
{
    [SerializeField] private float amplitude = 5f;   // UI는 픽셀 단위라 값이 좀 더 커도 됨
    [SerializeField] private float frequency = 0.8f;
    private RectTransform rt;
    private Vector2 basePos;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        basePos = rt.anchoredPosition;
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * frequency) * amplitude;
        rt.anchoredPosition = basePos + new Vector2(0f, y);
    }
}