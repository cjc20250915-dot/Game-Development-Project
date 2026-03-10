using UnityEngine;

public enum HealTargetMode
{
    LowestSingle = 0,
    AllAllies = 1
}

[System.Serializable]
public class ElementMatchEffectData
{
    [Header("Basic")]
    [Tooltip("元素类型编号，例如 type0 / type1 / type2 .")]
    public int elementType;

    [Tooltip("这个元素对应的消除效果类型")]
    public MatchEffectType effectType = MatchEffectType.None;

    [Header("Effect Values By Match Count")]
    [Tooltip("3消时的效果值")]
    public int value3 = 0;

    [Tooltip("4消时的效果值")]
    public int value4 = 0;

    [Tooltip("5消时的效果值")]
    public int value5 = 0;

    [Tooltip("6消时的效果值")]
    public int value6 = 0;

    [Header("Heal Target Mode By Match Count")]
    [Tooltip("3消时的治疗目标模式（仅治疗类型使用）")]
    public HealTargetMode healMode3 = HealTargetMode.LowestSingle;

    [Tooltip("4消时的治疗目标模式（仅治疗类型使用）")]
    public HealTargetMode healMode4 = HealTargetMode.LowestSingle;

    [Tooltip("5消时的治疗目标模式（仅治疗类型使用）")]
    public HealTargetMode healMode5 = HealTargetMode.AllAllies;

    [Tooltip("6消时的治疗目标模式（仅治疗类型使用）")]
    public HealTargetMode healMode6 = HealTargetMode.AllAllies;

    public int GetValueByMatchCount(int matchCount)
    {
        switch (matchCount)
        {
            case 3: return value3;
            case 4: return value4;
            case 5: return value5;
            case 6: return value6;
            default: return 0;
        }
    }

    public HealTargetMode GetHealModeByMatchCount(int matchCount)
    {
        switch (matchCount)
        {
            case 3: return healMode3;
            case 4: return healMode4;
            case 5: return healMode5;
            case 6: return healMode6;
            default: return HealTargetMode.LowestSingle;
        }
    }
}