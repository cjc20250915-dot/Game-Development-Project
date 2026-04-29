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

    [Header("Audio")]
    [Tooltip("鼠标进入（第一次展开面板）时播放；留空则不播")]
    [SerializeField] private AudioClip hoverSFX;
    [Tooltip("可为空：自动在本物体或子物体上找 AudioSource")]
    [SerializeField] private AudioSource audioSource;

    private Tween moveTween;
    private Tween fadeTween;

    private bool isShown = false;

    private void Awake()
    {
        EnsureAudioSource();

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

    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);
    }

    private void PlayHoverSFX()
    {
        if (hoverSFX == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(hoverSFX);
            return;
        }

        if (Camera.main != null)
            AudioSource.PlayClipAtPoint(hoverSFX, Camera.main.transform.position);
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

        PlayHoverSFX();

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