using System.Collections.Generic;
using UnityEngine;

/// <summary>技能治疗目标（友方）</summary>
public enum AllyHealTargetMode
{
    [Tooltip("施法者自己")]
    Self = 0,
    [Tooltip("槽位上 HP 更低的存活友方（同 HP 随机）")]
    LowestHpAlly = 1,
}

[CreateAssetMenu(menuName = "MatchTurnBattle/Skill Data", fileName = "SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;

    [TextArea]
    public string description;

    [Header("Heal (Ally)")]
    [Tooltip("技能是否为友方回血")]
    public bool dealsHeal = false;

    [Tooltip("回复量")]
    public int healAmount = 0;

    [Tooltip("回血作用在谁身上")]
    public AllyHealTargetMode healTargetMode = AllyHealTargetMode.Self;

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
    [Tooltip("是否随机选定前排目标。若为 false 且非 AOE，则需手动点击前排两名敌人之一")]
    public bool randomTarget = true;

    [Tooltip("是否为AOE技能（攻击前排所有存活敌人）")]
    public bool isAOE = false;

    [Header("Freeze")]
    [Tooltip("使命中敌人下一次敌方回合无法行动（与伤害的随机/AOE/手动选敌完全一致；可同时勾选伤害与本项）。防御等会在敌方回合开始时照常清除。")]
    public bool appliesFreeze = false;

    [Header("Skill Feedback — Freeze")]
    [Tooltip("施加冻结时播放一次（多目标时仍播一次）")]
    public AudioClip freezeSFX;
    [Tooltip("冻结期间持续存在，直到冻结解除时自动销毁（生成在敌人身上）")]
    public GameObject freezeVFXPrefab;

    [Header("Element Cost (use int type)")]
    [Tooltip("技能释放所需消耗的元素及数量")]
    public List<ElementCost> costs = new List<ElementCost>();

    [Header("Skill Feedback — Heal")]
    [Tooltip("触发回血效果时播放（在有回血配置且实际执行回血逻辑后）")]
    public AudioClip healSFX;

    [Tooltip("生成在承受回血的友方角色位置")]
    public GameObject healVFXPrefab;

    [Header("Skill Feedback — Self Damage")]
    [Tooltip("触发自残效果时播放")]
    public AudioClip selfHarmSFX;

    [Tooltip("生成在承担自伤的友方角色位置")]
    public GameObject selfHarmVFXPrefab;

    [Header("Skill Feedback — Enemy Damage")]
    [Tooltip("对敌人造成伤害时播放")]
    public AudioClip enemyDamageSFX;

    [Tooltip("生成在各受伤敌人的位置")]
    public GameObject enemyDamageVFXPrefab;
}
