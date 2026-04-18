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

    [Header("Breathing Camera")]
    public BreathingCamera breathingCamera;
    public bool disableBreathingWhileMoving = true;

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

        if (breathingCamera != null && targetCamera != null)
        {
            breathingCamera.SetBasePosition(targetCamera.position);
        }

        RefreshTextImmediate();
    }

    public void ToggleSlide()
    {
        ToggleSlideOnly();
        ToggleCameraOnly();
    }

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

    public void ToggleCameraOnly()
    {
        camTween?.Kill();

        if (targetCamera == null) return;

        Transform target = isAtA ? camPosB : camPosA;
        if (target == null) return;

        MoveCameraTo(target);
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

        if (breathingCamera != null && disableBreathingWhileMoving)
        {
            breathingCamera.SetBreathingEnabled(false);
        }

        camTween = DOTween.Sequence()
            .Append(targetCamera.DOMove(target.position, camMoveDuration))
            .Join(targetCamera.DORotateQuaternion(target.rotation, camMoveDuration))
            .SetEase(camEase)
            .OnComplete(() =>
            {
                if (breathingCamera != null)
                {
                    breathingCamera.SetBasePosition(targetCamera.position);

                    if (disableBreathingWhileMoving)
                        breathingCamera.SetBreathingEnabled(true);
                }
            });
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
        camTween?.Kill();

        moveTween = targetRect
            .DOAnchorPos(posA, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAtA = true;
                RefreshTextImmediate();
            });

        MoveCameraToA();
    }

    public void SlideOnlyToB()
    {
        if (targetRect == null) return;

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

        MoveCameraToA();
    }
}