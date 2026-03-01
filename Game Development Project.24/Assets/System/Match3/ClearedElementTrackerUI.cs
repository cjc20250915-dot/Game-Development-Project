using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

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

    /// <summary>当任意元素数量变化时触发（UI/按钮刷新可以用）</summary>
    public event Action OnCountsChanged;

    private void Awake()
    {
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
        OnCountsChanged?.Invoke();
    }

    /// <summary>重置所有统计（比如新回合开始）</summary>
    public void ResetAll()
    {
        for (int i = 0; i < counts.Length; i++)
            counts[i] = 0;

        RefreshAll();
        OnCountsChanged?.Invoke();
    }

    public int GetCount(int type)
    {
        if (type < 0 || type >= counts.Length) return 0;
        return counts[type];
    }

    /// <summary>是否足够支付一组技能消耗</summary>
    public bool CanSpend(List<ElementCost> costs)
    {
        if (costs == null || costs.Count == 0) return true;

        foreach (var c in costs)
        {
            if (GetCount(c.type) < c.amount)
                return false;
        }
        return true;
    }

    /// <summary>扣除一组技能消耗（成功返回 true）</summary>
    public bool Spend(List<ElementCost> costs)
    {
        if (!CanSpend(costs)) return false;

        if (costs == null || costs.Count == 0) return true;

        foreach (var c in costs)
        {
            if (c.type < 0 || c.type >= counts.Length) continue;

            counts[c.type] -= c.amount;
            if (counts[c.type] < 0) counts[c.type] = 0;

            Refresh(c.type);
        }

        OnCountsChanged?.Invoke();
        return true;
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