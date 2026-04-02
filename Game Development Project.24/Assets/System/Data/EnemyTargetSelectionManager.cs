using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTargetSelectionManager : MonoBehaviour
{
    public static EnemyTargetSelectionManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private EnemySlotBoard enemySlotBoard;

    private bool isSelectingTarget = false;
    private Action<EnemyUnit> onTargetSelected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (enemySlotBoard == null)
            enemySlotBoard = FindFirstObjectByType<EnemySlotBoard>();
    }

    /// <summary>
    /// 开始一次敌方目标选择
    /// </summary>
    public void BeginSelectEnemy(Action<EnemyUnit> callback)
    {
        if (enemySlotBoard == null)
        {
            Debug.LogWarning("[EnemyTargetSelectionManager] enemySlotBoard is null.");
            return;
        }

        isSelectingTarget = true;
        onTargetSelected = callback;

        Debug.Log("[EnemyTargetSelectionManager] Start selecting enemy target.");
    }

    /// <summary>
    /// 外部或取消按钮可调用
    /// </summary>
    public void CancelSelection()
    {
        isSelectingTarget = false;
        onTargetSelected = null;

        Debug.Log("[EnemyTargetSelectionManager] Target selection cancelled.");
    }

    /// <summary>
    /// 被 EnemyClickableTarget 调用
    /// </summary>
    public void TrySelectEnemy(EnemyUnit clickedEnemy)
    {
        if (!isSelectingTarget)
        {
            Debug.Log("[EnemyTargetSelectionManager] Not currently selecting target.");
            return;
        }

        if (clickedEnemy == null || clickedEnemy.IsDead)
        {
            Debug.Log("[EnemyTargetSelectionManager] Clicked enemy invalid.");
            return;
        }

        if (!IsEnemySelectable(clickedEnemy))
        {
            Debug.Log($"[EnemyTargetSelectionManager] {clickedEnemy.name} is not selectable. Only front row can be selected.");
            return;
        }

        Debug.Log($"[EnemyTargetSelectionManager] Selected target: {clickedEnemy.name}");

        isSelectingTarget = false;

        Action<EnemyUnit> callback = onTargetSelected;
        onTargetSelected = null;

        callback?.Invoke(clickedEnemy);
    }

    /// <summary>
    /// 目前规则：只能选前排
    /// </summary>
    private bool IsEnemySelectable(EnemyUnit enemy)
    {
        if (enemySlotBoard == null) return false;

        List<EnemyUnit> frontEnemies = enemySlotBoard.GetFrontRowAliveEnemies();
        if (frontEnemies == null || frontEnemies.Count == 0) return false;

        return frontEnemies.Contains(enemy);
    }

    public bool IsSelectingTarget()
    {
        return isSelectingTarget;
    }
}