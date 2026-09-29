using UnityEngine;
using DG.Tweening;

public class TutorialPrompt : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private string dismissActionId;
    [SerializeField] private float autoHideDelay = 4f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private KeyCode dismissKey = KeyCode.Return;
    private bool isShown;
    private Tween autoHideTween;

    private void Awake()
    {
        group.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isShown) return;
        if (Input.GetKeyDown(dismissKey)) Hide();
    }

    private void OnEnable() => TutorialMgr.Instance?.RegisterActivePrompt(this);
    private void OnDisable() => TutorialMgr.Instance?.UnregisterActivePrompt(this);

    public string DismissActionId => dismissActionId;

    public void Show()
    {
        if (isShown) return;
        if (GameManager.inst != null && GameManager.inst.HasSeenTutorial(dismissActionId)) return;

        GameManager.inst?.MarkTutorialSeen(dismissActionId);
        isShown = true;
        gameObject.SetActive(true);
        GameManager.inst?.AcquireFreeze();
        group.DOFade(1f, fadeDuration).SetUpdate(true);
        autoHideTween = DOVirtual.DelayedCall(autoHideDelay, Hide, true);
    }

    public void Hide()
    {
        if (!isShown) return;
        autoHideTween?.Kill();
        group.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
        {
            gameObject.SetActive(false);
            isShown = false;
            GameManager.inst?.ReleaseFreeze();
        });
    }
}