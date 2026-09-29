using UnityEngine;
using UnityEngine.UI;

public class GameSceneRefs : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button lastCheckBtn;

    private void Start()
    {
        GameManager.inst.RegisterSceneRefs(pausePanel, endPanel, retryBtn, lastCheckBtn);
                retryBtn.onClick.RemoveAllListeners();
        retryBtn.onClick.AddListener(GameManager.inst.ReGame);

        lastCheckBtn.onClick.RemoveAllListeners();
        lastCheckBtn.onClick.AddListener(GameManager.inst.LoadLastCheckpoint);
    }
}