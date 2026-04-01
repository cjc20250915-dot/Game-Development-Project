using UnityEngine;
using DG.Tweening;

public class ToggleSlideUI : MonoBehaviour
{
    [Header("要移动的目标")]
    public RectTransform targetRect;

    [Header("两个位置")]
    public Vector2 posA;
    public Vector2 posB;

    [Header("动画设置")]
    public float duration = 0.3f;
    public Ease moveEase = Ease.OutCubic;

    [Header("初始状态")]
    public bool startAtA = true;

    private bool isAtA = true;
    private Tween moveTween;

    private void Start()
    {
        if (targetRect == null)
        {
            Debug.LogWarning("ToggleSlideUI: targetRect 没有赋值。");
            return;
        }

        isAtA = startAtA;
        targetRect.anchoredPosition = isAtA ? posA : posB;
    }

    public void ToggleSlide()
    {
        if (targetRect == null) return;

        moveTween?.Kill();

        Vector2 targetPos = isAtA ? posB : posA;

        moveTween = targetRect
            .DOAnchorPos(targetPos, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAtA = !isAtA;
            });
    }

    public void SlideToA()
    {
        if (targetRect == null) return;

        moveTween?.Kill();

        moveTween = targetRect
            .DOAnchorPos(posA, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAtA = true;
            });
    }

    public void SlideToB()
    {
        if (targetRect == null) return;

        moveTween?.Kill();

        moveTween = targetRect
            .DOAnchorPos(posB, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAtA = false;
            });
    }
}