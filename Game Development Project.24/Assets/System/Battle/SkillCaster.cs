using System.Collections.Generic;
using UnityEngine;

public class SkillCaster : MonoBehaviour
{
    [Header("Element Tracker (type counts)")]
    [SerializeField] private ClearedElementTrackerUI_TMP tracker;

    [Header("Battle Refs")]
    [SerializeField] private EnemySlotBoard enemySlotBoard;
    [SerializeField] private AllySlotBoard allySlotBoard;
    [SerializeField] private EnemyTargetSelectionManager enemyTargetSelection;

    [Header("Owner")]
    [SerializeField] private AllyUnit owner;

    [Header("Skill Feedback Audio")]
    [Tooltip("为空时对 clip 使用 PlayClipAtPoint")]
    [SerializeField] private AudioSource skillFeedbackAudioSource;

    private void Awake()
    {
        if (enemySlotBoard == null)
            enemySlotBoard = FindFirstObjectByType<EnemySlotBoard>();

        if (allySlotBoard == null)
            allySlotBoard = FindFirstObjectByType<AllySlotBoard>();

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

        bool needsEnemyTargets = NeedsEnemyTargeting(skill);

        List<EnemyUnit> frontEnemies = null;
        if (needsEnemyTargets)
        {
            frontEnemies = enemySlotBoard.GetFrontRowAliveEnemies();
            if (frontEnemies == null || frontEnemies.Count == 0)
            {
                Debug.Log($"[SkillCaster] No alive front-row enemies. Cast failed: {skill.skillName}");
                return false;
            }
        }

        // 3) 单体手动选目标：需选中前排敌人（伤害或冻结）且 isAOE=false 且 randomTarget=false
        if (needsEnemyTargets && !skill.isAOE && !skill.randomTarget)
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

        AllyUnit healedUnit = ApplyHeal(skill);

        List<EnemyUnit> enemyTargetsResolved = new List<EnemyUnit>();
        if (needsEnemyTargets && frontEnemies != null)
            enemyTargetsResolved = ApplyEnemyDamageAndFreeze(skill, frontEnemies);

        if (skill.dealsSelfDamage && skill.selfDamageAmount > 0)
            ApplySelfDamage(skill.selfDamageAmount, skill.skillName);

        PlaySkillFeedback(skill, healedUnit, enemyTargetsResolved);

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

        AllyUnit healedUnit = ApplyHeal(skill);

        List<EnemyUnit> enemyTargetsResolved = new List<EnemyUnit>();
        if (NeedsEnemyTargeting(skill))
            enemyTargetsResolved.Add(target);

        ApplyEnemyEffectsToSingleTarget(skill, target);

        if (skill.dealsSelfDamage && skill.selfDamageAmount > 0)
            ApplySelfDamage(skill.selfDamageAmount, skill.skillName);

        PlaySkillFeedback(skill, healedUnit, enemyTargetsResolved);

        Debug.Log($"[SkillCaster] Cast success: {skill.skillName}");
    }

    private AllyUnit ApplyHeal(SkillData skill)
    {
        if (skill == null || !skill.dealsHeal || skill.healAmount <= 0)
            return null;

        AllyUnit target = ResolveHealTarget(skill);
        if (target == null || target.IsDead)
            return null;

        target.Heal(skill.healAmount);
        return target;
    }

    private AllyUnit ResolveHealTarget(SkillData skill)
    {
        if (owner == null)
            return null;

        switch (skill.healTargetMode)
        {
            case AllyHealTargetMode.Self:
                return owner;

            case AllyHealTargetMode.LowestHpAlly:
                return PickLowestHpAliveAlly();

            default:
                return owner;
        }
    }

    private AllyUnit PickLowestHpAliveAlly()
    {
        if (allySlotBoard == null)
            allySlotBoard = FindFirstObjectByType<AllySlotBoard>();

        if (allySlotBoard == null)
            return owner;

        AllyUnit a = allySlotBoard.SlotA;
        AllyUnit b = allySlotBoard.SlotB;

        List<AllyUnit> alive = new List<AllyUnit>(2);
        if (a != null && !a.IsDead) alive.Add(a);
        if (b != null && !b.IsDead) alive.Add(b);

        if (alive.Count == 0)
            return null;

        int lowest = int.MaxValue;
        foreach (var u in alive)
        {
            if (u.currentHP < lowest)
                lowest = u.currentHP;
        }

        List<AllyUnit> ties = new List<AllyUnit>();
        foreach (var u in alive)
        {
            if (u.currentHP == lowest)
                ties.Add(u);
        }

        return ties[Random.Range(0, ties.Count)];
    }

    private static bool NeedsEnemyTargeting(SkillData skill)
    {
        return skill != null && (skill.dealsDamage || skill.appliesFreeze);
    }

    /// <summary>随机单体：随机一人；AOE：前排全部存活。</summary>
    private List<EnemyUnit> ResolveFrontRowTargets(SkillData skill, List<EnemyUnit> frontEnemies)
    {
        List<EnemyUnit> list = new List<EnemyUnit>();

        if (frontEnemies == null || frontEnemies.Count == 0)
            return list;

        if (skill.isAOE)
        {
            for (int i = 0; i < frontEnemies.Count; i++)
            {
                EnemyUnit e = frontEnemies[i];
                if (e != null && !e.IsDead)
                    list.Add(e);
            }

            return list;
        }

        List<EnemyUnit> alive = new List<EnemyUnit>();
        for (int i = 0; i < frontEnemies.Count; i++)
        {
            EnemyUnit e = frontEnemies[i];
            if (e != null && !e.IsDead)
                alive.Add(e);
        }

        if (alive.Count == 0)
            return list;

        list.Add(alive[Random.Range(0, alive.Count)]);
        return list;
    }

    private List<EnemyUnit> ApplyEnemyDamageAndFreeze(SkillData skill, List<EnemyUnit> frontEnemies)
    {
        List<EnemyUnit> targets = ResolveFrontRowTargets(skill, frontEnemies);

        for (int i = 0; i < targets.Count; i++)
            ApplyEnemyEffectsToSingleTarget(skill, targets[i]);

        return targets;
    }

    private void ApplyEnemyEffectsToSingleTarget(SkillData skill, EnemyUnit enemy)
    {
        if (skill == null || enemy == null || enemy.IsDead)
            return;

        if (skill.dealsDamage)
        {
            enemy.TakeDamageFromSkill(skill.damageAmount, skill.enemyDamageVFXPrefab, skill.enemyDamageSFX);
            Debug.Log($"[SkillCaster] {skill.skillName} dealt {skill.damageAmount} damage to FRONT enemy: {enemy.name}");
        }

        if (skill.appliesFreeze)
            enemy.ScheduleFreezeNextEnemyPhase(skill.freezeVFXPrefab);
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

    /// <summary>
    /// 顺序：回血 → 自残 → 冻结音效。对敌伤害的 VFX/SFX 由 EnemyUnit.TakeDamageFromSkill 在扣血时播放，避免与默认受击重复。
    /// </summary>
    private void PlaySkillFeedback(SkillData skill, AllyUnit healedUnit, List<EnemyUnit> enemyTargetsResolved)
    {
        if (skill == null)
            return;

        if (skill.dealsHeal && skill.healAmount > 0 && healedUnit != null)
        {
            Vector3 pos = healedUnit.transform.position;
            PlayFeedbackSound(skill.healSFX, pos);
            SpawnFeedbackVFX(skill.healVFXPrefab, pos);
        }

        if (skill.dealsSelfDamage && skill.selfDamageAmount > 0 && owner != null)
        {
            Vector3 pos = owner.transform.position;
            PlayFeedbackSound(skill.selfHarmSFX, pos);
            SpawnFeedbackVFX(skill.selfHarmVFXPrefab, pos);
        }

        if (skill.appliesFreeze && enemyTargetsResolved != null && enemyTargetsResolved.Count > 0)
        {
            EnemyUnit sfxOrigin = null;
            for (int i = 0; i < enemyTargetsResolved.Count; i++)
            {
                EnemyUnit e = enemyTargetsResolved[i];
                if (e != null && e.HasFreezeScheduledForNextEnemyPhase)
                {
                    sfxOrigin = e;
                    break;
                }
            }

            if (sfxOrigin != null)
                PlayFeedbackSound(skill.freezeSFX, sfxOrigin.transform.position);
        }
    }

    private void SpawnFeedbackVFX(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        Instantiate(prefab, position, Quaternion.identity);
    }

    private void PlayFeedbackSound(AudioClip clip, Vector3 position)
    {
        if (clip == null)
            return;

        if (skillFeedbackAudioSource != null)
            skillFeedbackAudioSource.PlayOneShot(clip);
        else if (Camera.main != null)
            AudioSource.PlayClipAtPoint(clip, position);
    }
}
