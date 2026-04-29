using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂在「弹出层」根物体上（需有可射线检测的 Graphic），并与触发区的 <see cref="HoverShowUI"/> 配对，
/// 这样指针从触发区滑到弹出层时不会因为离开触发区而立刻收起。
/// </summary>
public class HoverShowUIExtendZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private HoverShowUI owner;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null)
            owner.NotifyExtendEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null)
            owner.NotifyExtendExit();
    }
}
