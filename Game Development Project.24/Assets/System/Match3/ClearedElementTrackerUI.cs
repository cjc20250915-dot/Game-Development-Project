using UnityEngine;
using TMPro;

public class ClearedElementTrackerUI_TMP : MonoBehaviour
{
    [System.Serializable]
    public class TypeUI
    {
        public int elementType;        // 例如 0~4
        public TMP_Text countText;     // 场景里对应这个元素的文本
    }

    [Header("Fixed UI Mapping (Type -> TMP_Text)")]
    public TypeUI[] typeUIs;

    private int[] counts;

    private void Awake()
    {
        // 初始化计数数组（按你提供的最大类型来）
        int maxType = -1;
        foreach (var t in typeUIs)
            if (t.elementType > maxType)
                maxType = t.elementType;

        counts = new int[maxType + 1];

        RefreshAll();
    }

    /// <summary>增加某种元素的消除数量</summary>
    public void Add(int type, int amount = 1)
    {
        if (type < 0 || type >= counts.Length) return;

        counts[type] += amount;
        Refresh(type);
    }

    /// <summary>重置所有统计（比如新回合开始）</summary>
    public void ResetAll()
    {
        for (int i = 0; i < counts.Length; i++)
            counts[i] = 0;

        RefreshAll();
    }

    public int GetCount(int type)
    {
        if (type < 0 || type >= counts.Length) return 0;
        return counts[type];
    }

    private void Refresh(int type)
    {
        foreach (var ui in typeUIs)
        {
            if (ui.elementType == type && ui.countText != null)
            {
                ui.countText.text = counts[type].ToString();
                return;
            }
        }
    }

    private void RefreshAll()
    {
        foreach (var ui in typeUIs)
        {
            if (ui.countText != null)
            {
                int type = ui.elementType;
                ui.countText.text = (type >= 0 && type < counts.Length)
                    ? counts[type].ToString()
                    : "0";
            }
        }
    }
}