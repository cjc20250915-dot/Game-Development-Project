using UnityEngine;
using DG.Tweening;
using TMPro;

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

    [Header("可选：要切换内容的文本UI")]
    public TMP_Text targetTextUI;

    [Header("对应A/B状态的文本")]
    [TextArea] public string textAtA = "A";
    [TextArea] public string textAtB = "B";

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

        RefreshTextImmediate();
    }

    public void ToggleSlide()
    {
        if (targetRect == null) return;

        moveTween?.Kill();

        bool willGoToA = !isAtA;
        Vector2 targetPos = isAtA ? posB : posA;

        moveTween = targetRect
            .DOAnchorPos(targetPos, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAtA = willGoToA;
                RefreshTextImmediate();
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
                RefreshTextImmediate();
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
                RefreshTextImmediate();
            });
    }

    private void RefreshTextImmediate()
    {
        if (targetTextUI == null) return;

        targetTextUI.text = isAtA ? textAtA : textAtB;
    }
}