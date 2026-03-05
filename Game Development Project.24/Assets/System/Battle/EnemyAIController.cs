using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    public enum EnemyActionType { Attack, Skill, Defend }

    [Header("Refs")]
    public TurnBattleManager turn;
    public AllySlotBoard allySlots;
    public List<EnemyUnit> enemies = new List<EnemyUnit>();

    [Header("Turn Order")]
    public bool higherSpeedFirst = true;


    [Header("Timing")]
    public float thinkDelay = 0.25f;
    public float betweenEnemiesDelay = 0.15f;

    private void Awake()
    {
        if (turn == null) turn = FindFirstObjectByType<TurnBattleManager>();
        if (allySlots == null) allySlots = FindFirstObjectByType<AllySlotBoard>();
    }

    private void OnEnable()
    {
        if (turn != null)
            turn.OnEnemyAIRequested += HandleEnemyAIRequested;
    }

    private void OnDisable()
    {
        if (turn != null)
            turn.OnEnemyAIRequested -= HandleEnemyAIRequested;
    }

    private void HandleEnemyAIRequested()
    {
        StopAllCoroutines();
        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        if (turn == null || allySlots == null)
        {
            Debug.LogWarning("[EnemyAI] Missing refs (turn/allySlots).");
            yield break;
        }

        // 建立行动顺序：按 speed，同速随机
        var order = BuildEnemyOrder(enemies, higherSpeedFirst);

        foreach (var enemy in order)
        {
            if (enemy == null || enemy.IsDead) continue;

            int times = Mathf.Max(1, enemy.actionsPerTurn);

            for (int i = 0; i < times; i++)
            {
                if (enemy == null || enemy.IsDead) break;

                yield return new WaitForSeconds(thinkDelay);

                var action = RollAction(enemy);
                yield return ExecuteAction(enemy, action);

                // 如果友方都死了可以提前结束（可选）
                if (AllAlliesDead()) break;
            }

            if (AllAlliesDead()) break;
            yield return new WaitForSeconds(betweenEnemiesDelay);
        }

        Debug.Log("[EnemyAI] Enemy actions finished.");

// 所有敌人都行动完 → 结束敌方回合 → 进入玩家回合
turn.EndEnemyTurn();
turn.BeginPlayerTurn();
    }

    // ===== Action Roll =====

private EnemyActionType RollAction(EnemyUnit enemy)
{
    // 归一化，防止总和不是1
    float a = Mathf.Max(0f, enemy.probAttack);
    float s = Mathf.Max(0f, enemy.probSkill);
    float d = Mathf.Max(0f, enemy.probDefend);

    float sum = a + s + d;
    if (sum <= 0.0001f) return EnemyActionType.Attack; // 兜底

    a /= sum; s /= sum; d /= sum;

    float r = Random.value;
    if (r < a) return EnemyActionType.Attack;
    if (r < a + s) return EnemyActionType.Skill;
    return EnemyActionType.Defend;
}

    // ===== Execute =====

    private IEnumerator ExecuteAction(EnemyUnit enemy, EnemyActionType action)
    {
        switch (action)
        {
            case EnemyActionType.Attack:
                DoAttack(enemy);
                yield break;

            case EnemyActionType.Skill:
                // TODO：回头写技能逻辑
                Debug.Log($"[EnemyAI] {enemy.name} uses SKILL (TODO)");
                yield break;

            case EnemyActionType.Defend:
                // TODO：回头写防御逻辑（加护盾/减伤等）
                Debug.Log($"[EnemyAI] {enemy.name} DEFENDS (TODO)");
                yield break;
        }
    }

    private void DoAttack(EnemyUnit enemy)
    {
        AllyUnit target = PickRandomAliveAlly();
        if (target == null)
        {
            Debug.Log("[EnemyAI] No alive ally to attack.");
            return;
        }

        int dmg = Mathf.Max(0, enemy.attackPower);

        // 这里假设 AllyUnit 有 TakeDamage(int)（如果你还没写，我下一步给你补）
        // 先用 SendMessage 兜底：没有方法也不会崩（但会慢一点）
        if (target.TryGetComponent(out MonoBehaviour _))
        {
            // 优先直接调用（如果你 AllyUnit 有 TakeDamage）
            var method = target.GetType().GetMethod("TakeDamage");
            if (method != null)
            {
                method.Invoke(target, new object[] { dmg });
            }
            else
            {
                // 兜底：如果你还没实现 TakeDamage，就先打印
                Debug.Log($"[EnemyAI] {enemy.name} ATTACKS {target.name} for {dmg} (Ally TakeDamage not found yet).");
            }
        }

        Debug.Log($"[EnemyAI] {enemy.name} ATTACK -> {target.name} dmg={dmg}");
    }

    // ===== Helpers =====

    private List<EnemyUnit> BuildEnemyOrder(List<EnemyUnit> list, bool highFirst)
    {
        var alive = list.Where(e => e != null && !e.IsDead)
                        .Select(e => new { enemy = e, tie = Random.value });

        return highFirst
            ? alive.OrderByDescending(x => x.enemy.speed).ThenBy(x => x.tie).Select(x => x.enemy).ToList()
            : alive.OrderBy(x => x.enemy.speed).ThenBy(x => x.tie).Select(x => x.enemy).ToList();
    }

    private AllyUnit PickRandomAliveAlly()
    {
        if (allySlots == null) return null;

        List<AllyUnit> candidates = new List<AllyUnit>(2);
        if (allySlots.SlotA != null && !allySlots.SlotA.IsDead) candidates.Add(allySlots.SlotA);
        if (allySlots.SlotB != null && !allySlots.SlotB.IsDead) candidates.Add(allySlots.SlotB);

        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool AllAlliesDead()
    {
        var a = allySlots != null ? allySlots.SlotA : null;
        var b = allySlots != null ? allySlots.SlotB : null;

        bool deadA = (a == null) || a.IsDead;
        bool deadB = (b == null) || b.IsDead;
        return deadA && deadB;
    }
}