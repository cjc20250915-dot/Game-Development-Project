using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在 UI Image 上：根据 AllySlotBoard 的 SlotA / SlotB 刷新为目标友军的 characterPortrait。
/// </summary>
public class AllyPortraitUIImageBinder : MonoBehaviour
{
    public enum AllySlotId
    {
        SlotA = 0,
        SlotB = 1,
    }

    [SerializeField] private AllySlotBoard allySlotBoard;

    [Tooltip("显示 SlotA 还是 SlotB 上的友军立绘")]
    [SerializeField] private AllySlotId slotId = AllySlotId.SlotA;

    [Tooltip("为空则使用本物体上的 Image")]
    [SerializeField] private Image targetImage;

    [Tooltip("槽位为空或友军未配置 characterPortrait 时使用")]
    [SerializeField] private Sprite emptySlotSprite;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (allySlotBoard == null)
            allySlotBoard = FindFirstObjectByType<AllySlotBoard>();
    }

    private void OnEnable()
    {
        if (allySlotBoard != null)
            allySlotBoard.OnSlotsChanged += RefreshPortrait;

        RefreshPortrait();
    }

    private void OnDisable()
    {
        if (allySlotBoard != null)
            allySlotBoard.OnSlotsChanged -= RefreshPortrait;
    }

    /// <summary>可在运行时切换跟随 A/B 槽后再调用。</summary>
    public void SetSlotId(AllySlotId id)
    {
        slotId = id;
        RefreshPortrait();
    }

    public void RefreshPortrait()
    {
        if (targetImage == null)
            return;

        AllyUnit ally = null;
        if (allySlotBoard != null)
            ally = slotId == AllySlotId.SlotA ? allySlotBoard.SlotA : allySlotBoard.SlotB;

        if (ally != null && ally.characterPortrait != null)
            targetImage.sprite = ally.characterPortrait;
        else if (emptySlotSprite != null)
            targetImage.sprite = emptySlotSprite;
        else
            targetImage.sprite = null;
    }
}
