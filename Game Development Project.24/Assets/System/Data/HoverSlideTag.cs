using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HoverSlideLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform target;   // 滑动面板
    [SerializeField] private CanvasGroup textCanvas; // 文字 CanvasGroup

    [Header("Positions")]
    [SerializeField] private Vector2 hiddenPos;
    [SerializeField] private Vector2 shownPos;

    [Header("Timing")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float fadeDuration = 0.2f;

    private Tween moveTween;
    private Tween fadeTween;

    private bool isShown = false;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        // 初始位置
        target.anchoredPosition = hiddenPos;

        // 初始透明
        if (textCanvas != null)
        {
            textCanvas.alpha = 0f;
            textCanvas.interactable = false;
            textCanvas.blocksRaycasts = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowLabel();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideLabel();
    }

    public void ShowLabel()
    {
        if (isShown) return;
        isShown = true;

        // 杀掉旧动画（防止快速进出抖动）
        moveTween?.Kill();
        fadeTween?.Kill();

        // 滑出
        moveTween = target.DOAnchorPos(shownPos, moveDuration).SetUpdate(true);

        // 淡入
        if (textCanvas != null)
        {
            textCanvas.interactable = true;
            textCanvas.blocksRaycasts = true;

            fadeTween = textCanvas.DOFade(1f, fadeDuration).SetUpdate(true);
        }
    }

    public void HideLabel()
    {
        if (!isShown) return;
        isShown = false;

        moveTween?.Kill();
        fadeTween?.Kill();

        // 滑回
        moveTween = target.DOAnchorPos(hiddenPos, moveDuration).SetUpdate(true);

        // 淡出
        if (textCanvas != null)
        {
            fadeTween = textCanvas.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (!isShown)
                    {
                        textCanvas.interactable = false;
                        textCanvas.blocksRaycasts = false;
                    }
                });
        }
    }

    // 方便按钮调用
    public void ToggleLabel()
    {
        if (isShown) HideLabel();
        else ShowLabel();
    }
}