using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class NotebookPageSwitcher : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private Transform pageA;   // 初始上方页面
    [SerializeField] private Transform pageB;   // 初始下方页面

    [Header("Final Slots")]
    [SerializeField] private Transform topPageSlot;
    [SerializeField] private Transform bottomPageSlot;

    [Header("Mid Points")]
    [SerializeField] private Transform topToBottomMidPoint;   // 上方页面翻到底部时的中间点
    [SerializeField] private Transform bottomToTopMidPoint;   // 下方页面翻到顶部时的中间点

    [Header("Layer Order")]
    [SerializeField] private int topSortingOrder = 20;
    [SerializeField] private int bottomSortingOrder = 10;

    [Header("Timing")]
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float fadeInDelay = 0.15f;

    [Header("Ease")]
    [SerializeField] private Ease moveEase = Ease.InOutSine;
    [SerializeField] private Ease rotateEase = Ease.InOutSine;

    [Header("Button")]
    [SerializeField] private Button switchButton;

    [Header("Canvas (Optional but Recommended)")]
    [SerializeField] private Canvas pageACanvas;
    [SerializeField] private Canvas pageBCanvas;

[Header("Fade Groups")]
[SerializeField] private CanvasGroup[] showWhenAOnTop;
[SerializeField] private CanvasGroup[] showWhenBOnTop;
[Header("Shared UI (hide briefly on every swap)")]
[SerializeField] private CanvasGroup[] sharedFadeGroups;
[SerializeField] private float sharedHideDuration = 0.12f;

    private Transform currentTopPage;
    private Transform currentBottomPage;

    private bool isAnimating = false;
    private bool isAOnTop = true;

    private void Awake()
    {
        currentTopPage = pageA;
        currentBottomPage = pageB;
        isAOnTop = true;

        SnapPages();
        ApplyLayerOrder();
        ApplyUIStateImmediate();

        if (switchButton != null)
            switchButton.onClick.AddListener(TogglePages);
    }

private void PlaySharedBriefHide()
{
    if (sharedFadeGroups == null) return;

    for (int i = 0; i < sharedFadeGroups.Length; i++)
    {
        CanvasGroup cg = sharedFadeGroups[i];
        if (cg == null) continue;

        cg.DOKill();
        cg.gameObject.SetActive(true);

        Sequence s = DOTween.Sequence();
        s.Append(cg.DOFade(0f, sharedHideDuration).SetEase(Ease.OutSine));
        s.AppendInterval(0.03f); // 稍微停一下更像被纸挡住
        s.Append(cg.DOFade(1f, sharedHideDuration).SetEase(Ease.InSine));
    }
}

    public void TogglePages()
    {
        if (isAnimating) return;
        if (currentTopPage == null || currentBottomPage == null) return;
        if (topPageSlot == null || bottomPageSlot == null) return;
        if (topToBottomMidPoint == null || bottomToTopMidPoint == null) return;

        isAnimating = true;
        SetButtonInteractable(false);

        Transform oldTop = currentTopPage;
        Transform oldBottom = currentBottomPage;

        // 动画开始前，先把即将上来的页面临时提到前面，避免穿帮
        BringPageToFront(oldBottom);

        Sequence seq = DOTween.Sequence();

        // ===== 第一段：移动到中间点 =====
        seq.AppendCallback(() =>
        {
            oldTop.SetAsLastSibling();
            oldBottom.SetAsLastSibling();
        });

        seq.Append(oldTop.DOMove(topToBottomMidPoint.position, moveDuration * 0.5f).SetEase(moveEase));
        oldTop.DORotate(topToBottomMidPoint.eulerAngles, moveDuration * 0.5f).SetEase(rotateEase);

        oldBottom.DOMove(bottomToTopMidPoint.position, moveDuration * 0.5f).SetEase(moveEase);
        oldBottom.DORotate(bottomToTopMidPoint.eulerAngles, moveDuration * 0.5f).SetEase(rotateEase);

        // ===== UI 淡入淡出，和第一页同步开始 =====
       // ===== UI 淡入淡出 =====
PlayFadeForSwap();
PlaySharedBriefHide();

        // ===== 第二段：移动到最终位置 =====
        seq.Append(oldTop.DOMove(bottomPageSlot.position, moveDuration * 0.5f).SetEase(moveEase));
        oldTop.DORotate(bottomPageSlot.eulerAngles, moveDuration * 0.5f).SetEase(rotateEase);

        oldBottom.DOMove(topPageSlot.position, moveDuration * 0.5f).SetEase(moveEase);
        oldBottom.DORotate(topPageSlot.eulerAngles, moveDuration * 0.5f).SetEase(rotateEase);

        seq.OnComplete(() =>
        {
            // 交换当前上下身份
            currentTopPage = oldBottom;
            currentBottomPage = oldTop;

            isAOnTop = (currentTopPage == pageA);

            // 强制对齐，避免累计误差
            currentTopPage.position = topPageSlot.position;
            currentTopPage.rotation = topPageSlot.rotation;

            currentBottomPage.position = bottomPageSlot.position;
            currentBottomPage.rotation = bottomPageSlot.rotation;

            ApplyLayerOrder();

            isAnimating = false;
            SetButtonInteractable(true);
        });
    }

    private void SnapPages()
    {
        if (currentTopPage != null && topPageSlot != null)
        {
            currentTopPage.position = topPageSlot.position;
            currentTopPage.rotation = topPageSlot.rotation;
        }

        if (currentBottomPage != null && bottomPageSlot != null)
        {
            currentBottomPage.position = bottomPageSlot.position;
            currentBottomPage.rotation = bottomPageSlot.rotation;
        }
    }

    private void ApplyLayerOrder()
    {
        if (pageACanvas != null)
        {
            pageACanvas.overrideSorting = true;
            pageACanvas.sortingOrder = isAOnTop ? topSortingOrder : bottomSortingOrder;
        }

        if (pageBCanvas != null)
        {
            pageBCanvas.overrideSorting = true;
            pageBCanvas.sortingOrder = isAOnTop ? bottomSortingOrder : topSortingOrder;
        }

        if (currentTopPage != null) currentTopPage.SetAsLastSibling();
        if (currentBottomPage != null) currentBottomPage.SetAsFirstSibling();
    }

    private void BringPageToFront(Transform page)
    {
        if (page == null) return;

        page.SetAsLastSibling();

        if (page == pageA && pageACanvas != null)
        {
            pageACanvas.overrideSorting = true;
            pageACanvas.sortingOrder = topSortingOrder + 1;
        }
        else if (page == pageB && pageBCanvas != null)
        {
            pageBCanvas.overrideSorting = true;
            pageBCanvas.sortingOrder = topSortingOrder + 1;
        }
    }

   private void PlayFadeForSwap()
{
    if (isAOnTop)
    {
        // 现在 A 在上面，切换后会变成 B 在上面
        FadeGroupArray(showWhenAOnTop, false);
        FadeGroupArray(showWhenBOnTop, true);
    }
    else
    {
        // 现在 B 在上面，切换后会变成 A 在上面
        FadeGroupArray(showWhenBOnTop, false);
        FadeGroupArray(showWhenAOnTop, true);
    }
}

private void ApplyUIStateImmediate()
{
    if (isAOnTop)
    {
        SetGroupImmediate(showWhenAOnTop, true);
        SetGroupImmediate(showWhenBOnTop, false);
    }
    else
    {
        SetGroupImmediate(showWhenAOnTop, false);
        SetGroupImmediate(showWhenBOnTop, true);
    }
}

private void FadeGroupArray(CanvasGroup[] groups, bool show)
{
    if (groups == null) return;

    for (int i = 0; i < groups.Length; i++)
    {
        CanvasGroup cg = groups[i];
        if (cg == null) continue;

        cg.DOKill();

        if (show)
        {
            // 需要淡入 → 先停顿再淡入
            cg.gameObject.SetActive(true);
            cg.interactable = false;
            cg.blocksRaycasts = false;

            Sequence s = DOTween.Sequence();

            s.AppendInterval(fadeInDelay);

            s.Append(
                cg.DOFade(1f, fadeDuration)
                  .SetEase(Ease.InOutSine)
            );

            s.OnComplete(() =>
            {
                if (cg != null)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            });
        }
        else
        {
            // 淡出 → 不停顿
            cg.interactable = false;
            cg.blocksRaycasts = false;

            cg.DOFade(0f, fadeDuration)
              .SetEase(Ease.InOutSine)
              .OnComplete(() =>
              {
                  if (cg != null)
                  {
                      cg.gameObject.SetActive(false);
                  }
              });
        }
    }
}

private void SetGroupImmediate(CanvasGroup[] groups, bool show)
{
    if (groups == null) return;

    for (int i = 0; i < groups.Length; i++)
    {
        CanvasGroup cg = groups[i];
        if (cg == null) continue;

        cg.DOKill();

        cg.gameObject.SetActive(show);
        cg.alpha = show ? 1f : 0f;
        cg.interactable = show;
        cg.blocksRaycasts = show;
    }
}

    private void SetButtonInteractable(bool value)
    {
        if (switchButton != null)
            switchButton.interactable = value;
    }
}