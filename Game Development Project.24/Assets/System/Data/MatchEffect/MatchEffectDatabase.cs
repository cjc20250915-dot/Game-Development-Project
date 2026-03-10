using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MatchEffectDatabase", menuName = "Match3Battle/Match Effect Database")]
public class MatchEffectDatabase : ScriptableObject
{
    [Header("All Element Match Effects")]
    public List<ElementMatchEffectData> effects = new List<ElementMatchEffectData>();

    public ElementMatchEffectData GetEffectData(int elementType)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null && effects[i].elementType == elementType)
                return effects[i];
        }

        return null;
    }

    public int GetEffectValue(int elementType, int matchCount)
    {
        ElementMatchEffectData data = GetEffectData(elementType);
        if (data == null) return 0;

        return data.GetValueByMatchCount(matchCount);
    }

    public MatchEffectType GetEffectType(int elementType)
    {
        ElementMatchEffectData data = GetEffectData(elementType);
        if (data == null) return MatchEffectType.None;

        return data.effectType;
    }
}