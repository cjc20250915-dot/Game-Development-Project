using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HoverSlideTag : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("要滑动的标签")]
    public RectTransform tagRect;

    [Header("位置设置")]
    public Vector2 hiddenPos;
    public Vector2 shownPos;

    [Header("动画设置")]
    public float duration = 0.25f;
    public Ease easeOut = Ease.OutCubic;
    public Ease easeBack = Ease.InCubic;

    [Header("可选")]
    public bool useFade = false;
    public CanvasGroup tagCanvasGroup;
    public float hiddenAlpha = 0f;
    public float shownAlpha = 1f;

    private Tween moveTween;
    private Tween fadeTween;

    private void Start()
    {
        if (tagRect == null)
        {
            Debug.LogWarning("HoverSlideTag: tagRect 没有赋值。");
            return;
        }

        tagRect.anchoredPosition = hiddenPos;

        if (useFade && tagCanvasGroup != null)
        {
            tagCanvasGroup.alpha = hiddenAlpha;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayShow();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayHide();
    }

    private void PlayShow()
    {
        if (tagRect == null) return;

        moveTween?.Kill();
        fadeTween?.Kill();

        moveTween = tagRect.DOAnchorPos(shownPos, duration).SetEase(easeOut);

        if (useFade && tagCanvasGroup != null)
        {
            fadeTween = tagCanvasGroup.DOFade(shownAlpha, duration);
        }
    }

    private void PlayHide()
    {
        if (tagRect == null) return;

        moveTween?.Kill();
        fadeTween?.Kill();

        moveTween = tagRect.DOAnchorPos(hiddenPos, duration).SetEase(easeBack);

        if (useFade && tagCanvasGroup != null)
        {
            fadeTween = tagCanvasGroup.DOFade(hiddenAlpha, duration);
        }
    }
}