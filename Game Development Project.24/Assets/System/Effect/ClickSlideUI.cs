using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 点击按钮时在两个 anchoredPosition 之间滑动目标 RectTransform，并播放音效。
/// 可将脚本挂在带 Button 的物体上（滑动自身），或为 Trigger Button 指定单独的 Slide Target。
/// </summary>
public class ClickSlideUI : MonoBehaviour
{
    public enum SlideToggleMode
    {
        [Tooltip("每次点击在 A ↔ B 之间切换")]
        ToggleAB,
        [Tooltip("每次点击都移动到 B")]
        AlwaysToB,
        [Tooltip("每次点击都移动到 A")]
        AlwaysToA,
    }

    [Header("Trigger")]
    [SerializeField]
    [Tooltip("留空则用本物体上的 Button")]
    private Button triggerButton;

    [Header("Target")]
    [SerializeField]
    [Tooltip("留空则用本物体上的 RectTransform")]
    private RectTransform slideTarget;

    [Header("Positions (Anchored)")]
    [SerializeField] private Vector2 positionA;
    [SerializeField] private Vector2 positionB;

    [SerializeField]
    [Tooltip("开始时停在 A 还是 B")]
    private bool startAtPositionA = true;

    [Header("Motion")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [SerializeField] private SlideToggleMode mode = SlideToggleMode.ToggleAB;

    [Header("Audio")]
    [SerializeField]
    [Tooltip("每次触发滑动时播放；留空则不播")]
    private AudioClip slideSFX;

    [SerializeField]
    [Tooltip("可为空：自动在本物体或子物体上查找 AudioSource")]
    private AudioSource audioSource;

    private Tween moveTween;
    private bool atEnd;

    private void Awake()
    {
        EnsureAudioSource();

        if (triggerButton == null)
            triggerButton = GetComponent<Button>();

        if (slideTarget == null)
            slideTarget = GetComponent<RectTransform>();

        if (slideTarget != null)
        {
            slideTarget.anchoredPosition = startAtPositionA ? positionA : positionB;
            atEnd = !startAtPositionA;
        }

        if (triggerButton != null)
            triggerButton.onClick.AddListener(OnClickSlide);
    }

    private void OnDestroy()
    {
        if (triggerButton != null)
            triggerButton.onClick.RemoveListener(OnClickSlide);

        moveTween?.Kill();
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);
    }

    private void PlaySlideSFX()
    {
        if (slideSFX == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(slideSFX);
            return;
        }

        if (Camera.main != null)
            AudioSource.PlayClipAtPoint(slideSFX, Camera.main.transform.position);
    }

    private void OnClickSlide()
    {
        Vector2 destination;

        switch (mode)
        {
            case SlideToggleMode.AlwaysToB:
                destination = positionB;
                atEnd = true;
                break;
            case SlideToggleMode.AlwaysToA:
                destination = positionA;
                atEnd = false;
                break;
            default:
                atEnd = !atEnd;
                destination = atEnd ? positionB : positionA;
                break;
        }

        MoveTo(destination);
    }

    /// <summary>可用 Button / UnityEvent 绑定。</summary>
    public void SlideToA()
    {
        atEnd = false;
        MoveTo(positionA);
    }

    public void SlideToB()
    {
        atEnd = true;
        MoveTo(positionB);
    }

    private void MoveTo(Vector2 anchored)
    {
        if (slideTarget == null)
            return;

        PlaySlideSFX();

        moveTween?.Kill();
        moveTween = slideTarget
            .DOAnchorPos(anchored, Mathf.Max(0.01f, moveDuration))
            .SetEase(ease)
            .SetUpdate(true);
    }
}
