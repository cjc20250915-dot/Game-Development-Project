using System;
using UnityEngine;

[Serializable]
public struct ElementCost
{
    [Tooltip("元素类型（与你当前系统的 type 对应，例如 0,1,2...）")]
    public int type;

    [Min(1)]
    [Tooltip("该元素消耗数量")]
    public int amount;
}