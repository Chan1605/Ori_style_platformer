using System.Collections;
using UnityEngine;

public class KeyGuideSequence : MonoBehaviour
{
    [System.Serializable]
    public class TutoSlide
    {
        public CanvasGroup guideGroup;
        public float fadeDuration = 0.3f;
    }

    [SerializeField] private TutoSlide[] slides;
    [SerializeField] private KeyCode nextKey = KeyCode.Return;
    [SerializeField] private KeyCode prevKey = KeyCode.Backspace;

    private int currentIndex = -1;
    private bool isTransitioning;
    private bool isActive;

    private void Start()
    {
        // 모든 슬라이드는 시작 시 꺼둔 상태로 정리
        foreach (var slide in slides)
        {
            slide.guideGroup.alpha = 0f;
            slide.guideGroup.gameObject.SetActive(false);
        }

        if (GameManager.inst != null && GameManager.inst.HasSeenKeyGuide)
        {
            return; // 이미 다 봤으면 아무것도 안 함
        }

        isActive = true;
        GameManager.inst?.AcquireFreeze();
        StartCoroutine(ShowSlideRoutine(0));
    }

    private void Update()
    {
        if (!isActive || isTransitioning) return;

        if (Input.GetKeyDown(nextKey))
        {
            if (currentIndex >= slides.Length - 1)
            {
                StartCoroutine(CloseGuideRoutine());
            }
            else
            {
                StartCoroutine(TransitionToSlide(currentIndex + 1));
            }
        }
        else if (Input.GetKeyDown(prevKey))
        {
            if (currentIndex > 0) // 첫 슬라이드에서는 더 못 돌아감
            {
                StartCoroutine(TransitionToSlide(currentIndex - 1));
            }
        }
    }

    private IEnumerator ShowSlideRoutine(int index)
    {
        currentIndex = index;
        var slide = slides[index];
        slide.guideGroup.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(slide.guideGroup, 0f, 1f, slide.fadeDuration));
    }

    private IEnumerator TransitionToSlide(int nextIndex)
    {
        isTransitioning = true;

        var current = slides[currentIndex];
        yield return StartCoroutine(Fade(current.guideGroup, current.guideGroup.alpha, 0f, current.fadeDuration));
        current.guideGroup.gameObject.SetActive(false);

        currentIndex = nextIndex;
        var next = slides[currentIndex];
        next.guideGroup.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(next.guideGroup, 0f, 1f, next.fadeDuration));

        isTransitioning = false;
    }

    private IEnumerator CloseGuideRoutine()
    {
        isTransitioning = true;

        var current = slides[currentIndex];
        yield return StartCoroutine(Fade(current.guideGroup, current.guideGroup.alpha, 0f, current.fadeDuration));
        current.guideGroup.gameObject.SetActive(false);

        isActive = false;
        GameManager.inst?.ReleaseFreeze();
        GameManager.inst?.MarkKeyGuideSeen();
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // timeScale=0이어도 정상 진행되도록
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }
}