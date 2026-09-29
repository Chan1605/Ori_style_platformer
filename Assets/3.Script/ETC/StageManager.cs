using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    [SerializeField] private GameObject stageBannerPanel;
    [SerializeField] private Text stageBannerText;
    [SerializeField] private Text stageinfoText;
    [SerializeField] private CanvasGroup stageBannerGroup;
    [SerializeField] private RectTransform stageBannerRect;
    [SerializeField] private float showDuration = 1.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float slideDistance = 30f;

    [Header("---- 보스 등장 연출 (임시) ----")]
    [SerializeField] private GameObject bossRevealImage;   // StagePanel 안의 보스 이미지
    [SerializeField] private Text specialMessageText;

    private Sequence bannerSequence;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void EnterStage(string stageName, string stageinfo, bool showBossReveal = false)
    {
        bannerSequence?.Kill();

        stageBannerText.text = stageName;
        stageinfoText.text = stageinfo;

        if (bossRevealImage != null)
            bossRevealImage.SetActive(showBossReveal);

        if (specialMessageText != null)
            specialMessageText.gameObject.SetActive(showBossReveal);

        stageBannerGroup.alpha = 0f;
        stageBannerRect.anchoredPosition = new Vector2(0f, -slideDistance);
        stageBannerPanel.SetActive(true);

        bannerSequence = DOTween.Sequence();
        bannerSequence.Append(stageBannerGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));
        bannerSequence.Join(stageBannerRect.DOAnchorPos(Vector2.zero, fadeInDuration).SetEase(Ease.OutBack));
        bannerSequence.AppendInterval(showDuration);
        bannerSequence.Append(stageBannerGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
        bannerSequence.Join(stageBannerRect.DOAnchorPos(new Vector2(0f, slideDistance), fadeOutDuration).SetEase(Ease.InQuad));
        bannerSequence.OnComplete(() =>
        {
            stageBannerPanel.SetActive(false);
            if (bossRevealImage != null) bossRevealImage.SetActive(false);
            if (specialMessageText != null) specialMessageText.gameObject.SetActive(false);
        });
    }
}