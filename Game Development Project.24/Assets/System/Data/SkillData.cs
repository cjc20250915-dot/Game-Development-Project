using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MatchTurnBattle/Skill Data", fileName = "SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;

    [TextArea]
    public string description;

    [Header("Damage")]
    [Tooltip("是否造成伤害")]
    public bool dealsDamage = true;

    [Tooltip("技能伤害量（如果不造成伤害，可忽略）")]
    public int damageAmount = 5;

    [Tooltip("是否随机选定目标（例如随机敌人）")]
    public bool randomTarget = true;

    [Header("Element Cost (use int type)")]
    [Tooltip("技能释放所需消耗的元素及数量")]
    public List<ElementCost> costs = new List<ElementCost>();
}