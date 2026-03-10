using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyWorldHealthBarManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySlotBoard enemySlotBoard;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private WorldHealthBar worldHealthBarPrefab;

    [Header("Optional")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 2f, 0f);

    private readonly Dictionary<EnemyUnit, WorldHealthBar> activeBars = new();

    private void OnEnable()
    {
        if (enemySlotBoard != null)
            enemySlotBoard.OnEnemiesChanged += RefreshBars;
    }

    private void OnDisable()
    {
        if (enemySlotBoard != null)
            enemySlotBoard.OnEnemiesChanged -= RefreshBars;
    }

    private void Start()
    {
        RefreshBars();
    }

    public void RefreshBars()
    {
        if (enemySlotBoard == null || targetCanvas == null || worldHealthBarPrefab == null)
            return;

        // 先清理失效敌人的血条
        List<EnemyUnit> toRemove = new List<EnemyUnit>();
        foreach (var kv in activeBars)
        {
            if (kv.Key == null || !enemySlotBoard.Enemies.Contains(kv.Key))
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);

                toRemove.Add(kv.Key);
            }
        }

        foreach (var enemy in toRemove)
            activeBars.Remove(enemy);

        // 给当前列表里没有血条的敌人创建血条
        foreach (EnemyUnit enemy in enemySlotBoard.Enemies)
        {
            if (enemy == null) continue;
            if (activeBars.ContainsKey(enemy)) continue;

            WorldHealthBar newBar = Instantiate(worldHealthBarPrefab, targetCanvas.transform);
            Debug.Log("创建血条: " + enemy.name);
            newBar.BindTarget(enemy, enemy.transform);

            activeBars.Add(enemy, newBar);

            // 可选：敌人死亡时移除血条
            enemy.OnDead += () => RemoveBar(enemy);
        }
    }

    private void RemoveBar(EnemyUnit enemy)
    {
        if (enemy == null) return;

        if (activeBars.TryGetValue(enemy, out WorldHealthBar bar))
        {
            if (bar != null)
                Destroy(bar.gameObject);

            activeBars.Remove(enemy);
        }
    }
}