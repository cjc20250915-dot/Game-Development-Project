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

    // ⭐ 摄像机相关
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

        // 摄像机初始位置
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

    public void ToggleSlide()
    {
        if (targetRect == null) return;

        moveTween?.Kill();
        camTween?.Kill();

        bool willGoToA = !isAtA;
        Vector2 targetPos = isAtA ? posB : posA;

        // UI滑动
        moveTween = targetRect
            .DOAnchorPos(targetPos, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAtA = willGoToA;
                RefreshTextImmediate();
            });

        // ⭐ 摄像机移动
        if (targetCamera != null)
        {
            Transform target = isAtA ? camPosB : camPosA;

            if (target != null)
            {
                camTween = DOTween.Sequence()
                    .Append(targetCamera.DOMove(target.position, camMoveDuration))
                    .Join(targetCamera.DORotateQuaternion(target.rotation, camMoveDuration))
                    .SetEase(camEase);
            }
        }
    }

    public void SlideToA()
    {
        moveTween?.Kill();
        camTween?.Kill();

        moveTween = targetRect
            .DOAnchorPos(posA, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAtA = true;
                RefreshTextImmediate();
            });

        MoveCameraTo(camPosA);
    }

    public void SlideToB()
    {
        moveTween?.Kill();
        camTween?.Kill();

        moveTween = targetRect
            .DOAnchorPos(posB, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAtA = false;
                RefreshTextImmediate();
            });

        MoveCameraTo(camPosB);
    }

    private void MoveCameraTo(Transform target)
    {
        if (targetCamera == null || target == null) return;

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
}