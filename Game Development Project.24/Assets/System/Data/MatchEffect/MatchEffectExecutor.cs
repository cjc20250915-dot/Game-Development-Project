using System.Collections.Generic;
using UnityEngine;

public class MatchEffectExecutor : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MatchEffectDatabase effectDatabase;

    [Header("References")]
    [SerializeField] private AllySlotBoard allySlotBoard;
    [SerializeField] private EnemySlotBoard enemySlotBoard;
    [SerializeField] private TurnBattleManager turnBattleManager;

    /// <summary>
    /// 外部调用入口：
    /// 传入本次消除的元素类型和消除数量（3/4/5/6）
    /// </summary>
    public void ExecuteEffect(int elementType, int matchCount)
    {
        if (effectDatabase == null)
        {
            Debug.LogWarning("[MatchEffectExecutor] effectDatabase is null.");
            return;
        }

        MatchEffectType effectType = effectDatabase.GetEffectType(elementType);
        int value = effectDatabase.GetEffectValue(elementType, matchCount);

        if (value <= 0)
        {
            Debug.Log($"[MatchEffectExecutor] elementType={elementType}, matchCount={matchCount}, value <= 0, skip.");
            return;
        }

        switch (effectType)
        {
            case MatchEffectType.FrontRandomEnemyDamage:
                ExecuteFrontRandomEnemyDamage(value);
                break;

            case MatchEffectType.FrontAllEnemiesDamage:
                ExecuteFrontAllEnemiesDamage(value);
                break;

            case MatchEffectType.HealLowestAlly:
                ExecuteHealLowestAlly(value);
                break;

            case MatchEffectType.RestoreMoves:
                ExecuteRestoreMoves(value);
                break;

            case MatchEffectType.Reserved:
                Debug.Log($"[MatchEffectExecutor] elementType={elementType} is Reserved. No effect yet.");
                break;

            case MatchEffectType.None:
            default:
                Debug.Log($"[MatchEffectExecutor] elementType={elementType} has no effect.");
                break;
        }
    }

    private void ExecuteFrontRandomEnemyDamage(int damage)
    {
        if (enemySlotBoard == null)
        {
            Debug.LogWarning("[MatchEffectExecutor] enemySlotBoard is null.");
            return;
        }

        List<EnemyUnit> frontEnemies = enemySlotBoard.GetFrontRowAliveEnemies();
        if (frontEnemies == null || frontEnemies.Count == 0)
        {
            Debug.Log("[MatchEffectExecutor] No alive front enemies.");
            return;
        }

        EnemyUnit target = frontEnemies[Random.Range(0, frontEnemies.Count)];
        if (target == null) return;

        target.TakeDamage(damage);
        Debug.Log($"[MatchEffectExecutor] Front random enemy took {damage} damage: {target.name}");
    }

    private void ExecuteFrontAllEnemiesDamage(int damage)
    {
        if (enemySlotBoard == null)
        {
            Debug.LogWarning("[MatchEffectExecutor] enemySlotBoard is null.");
            return;
        }

        List<EnemyUnit> frontEnemies = enemySlotBoard.GetFrontRowAliveEnemies();
        if (frontEnemies == null || frontEnemies.Count == 0)
        {
            Debug.Log("[MatchEffectExecutor] No alive front enemies.");
            return;
        }

        foreach (EnemyUnit enemy in frontEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            enemy.TakeDamage(damage);
            Debug.Log($"[MatchEffectExecutor] Front enemy took {damage} AOE damage: {enemy.name}");
        }
    }

    private void ExecuteHealLowestAlly(int healAmount)
    {
        if (allySlotBoard == null)
        {
            Debug.LogWarning("[MatchEffectExecutor] allySlotBoard is null.");
            return;
        }

        List<AllyUnit> candidates = GetAliveAllies();
        if (candidates.Count == 0)
        {
            Debug.Log("[MatchEffectExecutor] No alive allies.");
            return;
        }

        int lowestHP = int.MaxValue;
        List<AllyUnit> lowestAllies = new List<AllyUnit>();

        for (int i = 0; i < candidates.Count; i++)
        {
            AllyUnit ally = candidates[i];
            if (ally == null || ally.IsDead) continue;

            if (ally.currentHP < lowestHP)
            {
                lowestHP = ally.currentHP;
                lowestAllies.Clear();
                lowestAllies.Add(ally);
            }
            else if (ally.currentHP == lowestHP)
            {
                lowestAllies.Add(ally);
            }
        }

        if (lowestAllies.Count == 0)
        {
            Debug.Log("[MatchEffectExecutor] No valid ally target for healing.");
            return;
        }

        AllyUnit target = lowestAllies[Random.Range(0, lowestAllies.Count)];
        target.Heal(healAmount);

        Debug.Log($"[MatchEffectExecutor] Heal lowest ally: {target.name}, +{healAmount} HP");
    }

    private void ExecuteRestoreMoves(int amount)
    {
        if (turnBattleManager == null)
        {
            Debug.LogWarning("[MatchEffectExecutor] turnBattleManager is null.");
            return;
        }

        turnBattleManager.RestoreMoves(amount);
        Debug.Log($"[MatchEffectExecutor] Restored {amount} moves.");
    }

    private List<AllyUnit> GetAliveAllies()
    {
        List<AllyUnit> result = new List<AllyUnit>();

        if (allySlotBoard == null) return result;

        if (allySlotBoard.SlotA != null && !allySlotBoard.SlotA.IsDead)
            result.Add(allySlotBoard.SlotA);

        if (allySlotBoard.SlotB != null && !allySlotBoard.SlotB.IsDead)
            result.Add(allySlotBoard.SlotB);

        return result;
    }
}