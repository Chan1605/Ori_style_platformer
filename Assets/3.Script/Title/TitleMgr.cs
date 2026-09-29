using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleMgr : MonoBehaviour
{
    [Header("Press Any Key")]
    [SerializeField] private GameObject pressAnyKeyObj;
    [SerializeField] private Graphic pressAnyKeyGraphic;
    [SerializeField] private float blinkSpeed = 1.5f;
    [SerializeField] private float minAlpha = 0.3f;

    [Header("Menu")]
    [SerializeField] private GameObject menuGroup;

    [Header("Scene")]
    [SerializeField] private string playSceneName;

    private bool waitingForInput = true;

    private void Start()
    {
        CustomCursor.Instance?.SetVisible(true);
        pressAnyKeyObj.SetActive(true);
        menuGroup.SetActive(false);
    }

    private void Update()
    {
        if (!waitingForInput) return;

        Blink(pressAnyKeyGraphic);

        if (Input.anyKeyDown) 
        {
            waitingForInput = false;
            pressAnyKeyObj.SetActive(false);
            menuGroup.SetActive(true);
        }
    }

    private void Blink(Graphic graphic)
    {
        if (graphic == null) return;
        float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
        Color c = graphic.color;
        c.a = Mathf.Lerp(minAlpha, 1f, t);
        graphic.color = c;
    }

    public void OnClickStartGame()
    {
        SceneTransitionMgr.Instance.LoadScene(playSceneName);
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; 
#else
        Application.Quit();
#endif
    }
}