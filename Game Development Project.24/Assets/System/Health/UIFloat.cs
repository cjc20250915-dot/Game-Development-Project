using UnityEngine;
using DG.Tweening;

public class UIFloat : MonoBehaviour
{
    [Header("目标")]
    public RectTransform target;

    [Header("浮动设置")]
    public float distance = 20f;     // 上下幅度（像素）
    public float duration = 1.5f;    // 单程时间

    [Header("动画曲线")]
    public Ease ease = Ease.InOutSine;

    private Vector2 startPos;
    private Tween floatTween;

    private void Start()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        startPos = target.anchoredPosition;

        PlayFloat();
    }

    private void PlayFloat()
    {
        floatTween?.Kill();

        floatTween = target.DOAnchorPosY(startPos.y + distance, duration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo); // 无限来回
    }

    private void OnDisable()
    {
        floatTween?.Kill();
    }
}