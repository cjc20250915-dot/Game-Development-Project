using System;
using UnityEngine;

public class SkillCaster : MonoBehaviour
{
    [Header("Element Tracker (type counts)")]
    public ClearedElementTrackerUI_TMP tracker;

    /// <summary>技能造成伤害时，交给战斗系统处理（谁掉血/随机目标等）</summary>
    public event Action<int, bool> OnRequestDealDamage; // damage, randomTarget

    public bool CanCast(SkillData skill)
    {
        if (skill == null || tracker == null) return false;
        return tracker.CanSpend(skill.costs);
    }

    public bool TryCast(SkillData skill)
    {
        if (skill == null) return false;

        if (tracker == null)
        {
            Debug.LogWarning("[SkillCaster] tracker is null. Please assign ClearedElementTrackerUI_TMP in Inspector.");
            return false;
        }

        // 1) 检查元素是否足够
        if (!CanCast(skill))
        {
            Debug.Log($"[SkillCaster] Not enough elements to cast {skill.skillName}");
            return false;
        }

        // 2) 扣除元素
        if (!tracker.Spend(skill.costs))
        {
            Debug.LogWarning($"[SkillCaster] Spend failed for {skill.skillName}");
            return false;
        }

        // 3) 执行技能效果（现在先做伤害）
        if (skill.dealsDamage)
        {
            OnRequestDealDamage?.Invoke(skill.damageAmount, skill.randomTarget);
        }

        Debug.Log($"[SkillCaster] Cast success: {skill.skillName}");
        return true;
    }
}