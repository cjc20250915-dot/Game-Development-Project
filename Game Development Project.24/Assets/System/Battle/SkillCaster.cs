using System.Collections.Generic;
using UnityEngine;

public class SkillCaster : MonoBehaviour
{
    [Header("Element Tracker (type counts)")]
    [SerializeField] private ClearedElementTrackerUI_TMP tracker;

    [Header("Battle Refs")]
    [SerializeField] private EnemySlotBoard enemySlotBoard;
    [SerializeField] private EnemyTargetSelectionManager enemyTargetSelection;

    [Header("Owner")]
    [SerializeField] private AllyUnit owner;

    private void Awake()
    {
        if (enemySlotBoard == null)
            enemySlotBoard = FindFirstObjectByType<EnemySlotBoard>();

        if (enemyTargetSelection == null)
            enemyTargetSelection = FindFirstObjectByType<EnemyTargetSelectionManager>();

        if (tracker == null)
            tracker = FindFirstObjectByType<ClearedElementTrackerUI_TMP>();

        if (owner == null)
            owner = GetComponent<AllyUnit>();

        if (owner == null)
            owner = GetComponentInParent<AllyUnit>();

        if (owner == null)
            owner = GetComponentInChildren<AllyUnit>();
    }

    public bool CanCast(SkillData skill)
    {
        if (skill == null || tracker == null) return false;
        return tracker.CanSpend(skill.costs);
    }

    public bool TryCast(SkillData skill)
    {
        if (skill == null)
        {
            Debug.LogWarning("[SkillCaster] skill is null.");
            return false;
        }

        if (tracker == null)
        {
            Debug.LogWarning("[SkillCaster] tracker is null.");
            return false;
        }

        if (enemySlotBoard == null)
        {
            Debug.LogWarning("[SkillCaster] enemySlotBoard is null.");
            return false;
        }

        // 1) 检查元素是否足够
        if (!CanCast(skill))
        {
            Debug.Log($"[SkillCaster] Not enough elements to cast {skill.skillName}");
            return false;
        }

        // 2) 找前排目标（前排两格之一）
        List<EnemyUnit> frontEnemies = enemySlotBoard.GetFrontRowAliveEnemies();
        if (frontEnemies == null || frontEnemies.Count == 0)
        {
            Debug.Log($"[SkillCaster] No alive front-row enemies. Cast failed: {skill.skillName}");
            return false;
        }

        // 3) 单体手动选目标：isAOE=false 且 randomTarget=false，点击前排敌人后结算
        if (!skill.isAOE && !skill.randomTarget)
        {
            if (enemyTargetSelection == null)
            {
                Debug.LogWarning($"[SkillCaster] EnemyTargetSelectionManager missing, cannot select target for {skill.skillName}");
                return false;
            }

            if (enemyTargetSelection.IsSelectingTarget())
            {
                Debug.Log($"[SkillCaster] Already selecting an enemy target. Cancel or finish first.");
                return false;
            }

            enemyTargetSelection.BeginSelectEnemy(selectedEnemy =>
            {
                CompleteManualSingleTargetCast(skill, selectedEnemy);
            });

            Debug.Log($"[SkillCaster] Select a front-row enemy for {skill.skillName}");
            return true;
        }

        // 4) 扣除元素
        if (!tracker.Spend(skill.costs))
        {
            Debug.LogWarning($"[SkillCaster] Spend failed for {skill.skillName}");
            return false;
        }

        // 5) 执行对敌伤害
        if (skill.dealsDamage)
        {
            if (skill.isAOE)
            {
                CastFrontRowAOE(skill.damageAmount, frontEnemies, skill.skillName);
            }
            else
            {
                CastFrontRowRandom(skill.damageAmount, frontEnemies, skill.skillName);
            }
        }

        // 6) 执行自伤
        if (skill.dealsSelfDamage && skill.selfDamageAmount > 0)
        {
            ApplySelfDamage(skill.selfDamageAmount, skill.skillName);
        }

        Debug.Log($"[SkillCaster] Cast success: {skill.skillName}");
        return true;
    }

    /// <summary>
    /// 手动点选前排单体后结算（此时才扣元素）
    /// </summary>
    private void CompleteManualSingleTargetCast(SkillData skill, EnemyUnit target)
    {
        if (skill == null || target == null || target.IsDead)
        {
            Debug.LogWarning("[SkillCaster] Manual cast aborted: invalid target.");
            return;
        }

        if (enemySlotBoard == null || tracker == null)
            return;

        List<EnemyUnit> front = enemySlotBoard.GetFrontRowAliveEnemies();
        if (front == null || !front.Contains(target))
        {
            Debug.LogWarning("[SkillCaster] Selected enemy is not a valid front-row target anymore.");
            return;
        }

        if (!CanCast(skill))
        {
            Debug.Log($"[SkillCaster] Not enough elements to finish {skill.skillName}");
            return;
        }

        if (!tracker.Spend(skill.costs))
        {
            Debug.LogWarning($"[SkillCaster] Spend failed for {skill.skillName}");
            return;
        }

        if (skill.dealsDamage)
        {
            target.TakeDamage(skill.damageAmount);
            Debug.Log($"[SkillCaster] {skill.skillName} dealt {skill.damageAmount} damage to selected FRONT enemy: {target.name}");
        }

        if (skill.dealsSelfDamage && skill.selfDamageAmount > 0)
        {
            ApplySelfDamage(skill.selfDamageAmount, skill.skillName);
        }

        Debug.Log($"[SkillCaster] Cast success: {skill.skillName}");
    }

    private void CastFrontRowRandom(int damage, List<EnemyUnit> frontEnemies, string skillName)
    {
        if (frontEnemies == null || frontEnemies.Count == 0) return;

        EnemyUnit target = frontEnemies[Random.Range(0, frontEnemies.Count)];
        if (target == null || target.IsDead) return;

        target.TakeDamage(damage);
        Debug.Log($"[SkillCaster] {skillName} dealt {damage} damage to random FRONT enemy: {target.name}");
    }

    private void CastFrontRowAOE(int damage, List<EnemyUnit> frontEnemies, string skillName)
    {
        if (frontEnemies == null || frontEnemies.Count == 0) return;

        for (int i = 0; i < frontEnemies.Count; i++)
        {
            EnemyUnit enemy = frontEnemies[i];
            if (enemy == null || enemy.IsDead) continue;

            enemy.TakeDamage(damage);
            Debug.Log($"[SkillCaster] {skillName} dealt {damage} AOE damage to FRONT enemy: {enemy.name}");
        }
    }

    private void ApplySelfDamage(int damage, string skillName)
    {
        if (owner == null)
        {
            Debug.LogWarning($"[SkillCaster] owner is null, cannot apply self damage for {skillName}");
            return;
        }

        if (owner.IsDead) return;

        owner.TakeDamage(damage);
        Debug.Log($"[SkillCaster] {skillName} dealt {damage} self-damage to {owner.displayName}");
    }
}