using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MatchTurnBattle/Skill Data", fileName = "SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;

    [TextArea]
    public string description;

    [Header("Damage Settings")]
    [Tooltip("技能是否对敌人造成伤害")]
    public bool dealsDamage = true;

    [Tooltip("技能对敌人造成的伤害量")]
    public int damageAmount = 5;

    [Header("Self Damage")]
    [Tooltip("技能是否对自己造成伤害")]
    public bool dealsSelfDamage = false;

    [Tooltip("技能对自己造成的伤害量")]
    public int selfDamageAmount = 0;

    [Header("Target Settings")]
    [Tooltip("是否随机选定目标")]
    public bool randomTarget = true;

    [Tooltip("是否为AOE技能（攻击所有敌人）")]
    public bool isAOE = false;

    [Header("Element Cost (use int type)")]
    [Tooltip("技能释放所需消耗的元素及数量")]
    public List<ElementCost> costs = new List<ElementCost>();
}