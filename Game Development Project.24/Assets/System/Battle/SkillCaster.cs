using UnityEngine;

public class SkillCaster : MonoBehaviour
{
    // 先留接口：之后你接元素消耗/敌人扣血再扩展
    public bool TryCast(SkillData skill)
    {
        if (skill == null) return false;

        Debug.Log($"[SkillCaster] Cast: {skill.skillName}");
        return true;
    }
}