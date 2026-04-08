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

    [Header("Camera Move")]
    public Transform targetCamera;
    public Transform camPosA;
    public Transform camPosB;
    public float camMoveDuration = 0.5f;
    public Ease camEase = Ease.OutCubic;

    private bool isAtA = true;
    private Tween moveTween;
    private Tween camTween;

    private void Start()
    {
        if (targetRect == null)
        {
            Debug.LogWarning("ToggleSlideUI: targetRect 没有赋值。");
            return;
        }

        isAtA = startAtA;
        targetRect.anchoredPosition = isAtA ? posA : posB;

        if (targetCamera != null)
        {
            Transform startPos = isAtA ? camPosA : camPosB;
            if (startPos != null)
            {
                targetCamera.position = startPos.position;
                targetCamera.rotation = startPos.rotation;
            }
        }

        RefreshTextImmediate();
    }

    // 原本的：UI + 文本 + 摄像机
    public void ToggleSlide()
    {
        ToggleSlideOnly();
        ToggleCameraOnly();
    }

    // 新增：只切 UI + 文本
    public void ToggleSlideOnly()
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

    // 新增：只切摄像机
    public void ToggleCameraOnly()
    {
        camTween?.Kill();

        if (targetCamera == null) return;

        Transform target = isAtA ? camPosB : camPosA;
        if (target == null) return;

        camTween = DOTween.Sequence()
            .Append(targetCamera.DOMove(target.position, camMoveDuration))
            .Join(targetCamera.DORotateQuaternion(target.rotation, camMoveDuration))
            .SetEase(camEase);
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

    public void MoveCameraToA()
    {
        MoveCameraTo(camPosA);
    }

    public void MoveCameraToB()
    {
        MoveCameraTo(camPosB);
    }

    private void MoveCameraTo(Transform target)
    {
        if (targetCamera == null || target == null) return;

        camTween?.Kill();

        camTween = DOTween.Sequence()
            .Append(targetCamera.DOMove(target.position, camMoveDuration))
            .Join(targetCamera.DORotateQuaternion(target.rotation, camMoveDuration))
            .SetEase(camEase);
    }

    private void RefreshTextImmediate()
    {
        if (targetTextUI == null) return;
        targetTextUI.text = isAtA ? textAtA : textAtB;
    }

public void SlideOnlyToA()
{
    if (targetRect == null) return;

    moveTween?.Kill();
    camTween?.Kill(); // ⭐ 也要停掉摄像机旧动画

    moveTween = targetRect
        .DOAnchorPos(posA, duration)
        .SetEase(moveEase)
        .OnComplete(() =>
        {
            isAtA = true;
            RefreshTextImmediate();
        });

    // ⭐ 每次都把摄像机移动到 A
    MoveCameraToA();
}

public void SlideOnlyToB()
{
    if (targetRect == null) return;

    moveTween?.Kill();
    camTween?.Kill(); // ⭐ 防止叠动画

    moveTween = targetRect
        .DOAnchorPos(posB, duration)
        .SetEase(moveEase)
        .OnComplete(() =>
        {
            isAtA = false;
            RefreshTextImmediate();
        });

    // ⭐ 注意：这里也移动到 A（不是 B）
    MoveCameraToA();
}
}