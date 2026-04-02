using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyWorldStatusUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySlotBoard enemySlotBoard;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private EnemyWorldStatusUI statusUIPrefab;

    [Header("Optional")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 2.2f, 0f);

    private readonly Dictionary<EnemyUnit, EnemyWorldStatusUI> activeUIs = new();

    private void OnEnable()
    {
        if (enemySlotBoard != null)
            enemySlotBoard.OnEnemiesChanged += RefreshStatusUIs;
    }

    private void OnDisable()
    {
        if (enemySlotBoard != null)
            enemySlotBoard.OnEnemiesChanged -= RefreshStatusUIs;
    }

    private void Start()
    {
        RefreshStatusUIs();
    }

    public void RefreshStatusUIs()
    {
        if (enemySlotBoard == null || targetCanvas == null || statusUIPrefab == null)
            return;

        // 先清理失效敌人的UI
        List<EnemyUnit> toRemove = new List<EnemyUnit>();
        foreach (var kv in activeUIs)
        {
            if (kv.Key == null || !enemySlotBoard.Enemies.Contains(kv.Key))
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);

                toRemove.Add(kv.Key);
            }
        }

        foreach (var enemy in toRemove)
            activeUIs.Remove(enemy);

        // 给当前没有UI的敌人生成状态UI
        foreach (EnemyUnit enemy in enemySlotBoard.Enemies)
        {
            if (enemy == null) continue;
            if (activeUIs.ContainsKey(enemy)) continue;

            EnemyWorldStatusUI newUI = Instantiate(statusUIPrefab, targetCanvas.transform);
            newUI.Bind(enemy, enemy.transform, defaultOffset);

            activeUIs.Add(enemy, newUI);

            enemy.OnDead += () => RemoveStatusUI(enemy);
        }
    }

    private void RemoveStatusUI(EnemyUnit enemy)
    {
        if (enemy == null) return;

        if (activeUIs.TryGetValue(enemy, out EnemyWorldStatusUI ui))
        {
            if (ui != null)
                Destroy(ui.gameObject);

            activeUIs.Remove(enemy);
        }
    }
}