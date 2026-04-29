using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 悬停在本物体（需有可射线检测的 Graphic，例如透明 Image）上时显示另一个 UI；
/// 鼠标离开时可延迟收起，便于移到弹出层上点击。
/// </summary>
public class HoverShowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    [Tooltip("进入悬停时要显示的物体（一般会先设为初始隐藏）")]
    private GameObject uiToShow;

    [SerializeField]
    private bool hideOnPointerExit = true;

    [SerializeField]
    [Tooltip("指针离开后延迟这么久再隐藏；若要到弹出层上操作，建议 0.05～0.2，并在弹出层加 HoverShowUIExtendZone")]
    private float hideDelaySeconds = 0.12f;

    private int hoverDepth;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        // 运行时先把目标收起；若在预制体里需要调试可见性，可在编辑器运行后再勾回来。
        if (uiToShow != null)
            uiToShow.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyHoverDelta(+1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!hideOnPointerExit)
            return;

        ApplyHoverDelta(-1);
    }

    /// <summary>挂在弹出层上的 HoverShowUIExtendZone 会调用，避免触发区与面板不相叠时误收起。</summary>
    internal void NotifyExtendEnter()
    {
        ApplyHoverDelta(+1);
    }

    internal void NotifyExtendExit()
    {
        if (!hideOnPointerExit)
            return;

        ApplyHoverDelta(-1);
    }

    private void ApplyHoverDelta(int delta)
    {
        hoverDepth += delta;
        if (hoverDepth < 0)
            hoverDepth = 0;

        if (delta > 0)
        {
            CancelScheduledHide();
            ShowNow();
            return;
        }

        if (hoverDepth <= 0)
            ScheduleHide();
    }

    private void ShowNow()
    {
        if (uiToShow == null)
            return;

        uiToShow.SetActive(true);
    }

    private void HideNow()
    {
        if (uiToShow == null)
            return;

        uiToShow.SetActive(false);
    }

    private void CancelScheduledHide()
    {
        if (hideCoroutine == null)
            return;

        StopCoroutine(hideCoroutine);
        hideCoroutine = null;
    }

    private void ScheduleHide()
    {
        CancelScheduledHide();

        if (hideDelaySeconds <= 0f)
        {
            hoverDepth = 0;
            HideNow();
            return;
        }

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(hideDelaySeconds);

        hideCoroutine = null;
        hoverDepth = 0;
        HideNow();
    }

    /// <summary>外部如需手动收起（例如打开了全屏菜单）。</summary>
    public void ForceHide()
    {
        CancelScheduledHide();
        hoverDepth = 0;
        HideNow();
    }
}
