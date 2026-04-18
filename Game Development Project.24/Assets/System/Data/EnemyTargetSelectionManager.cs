using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyTargetSelectionManager : MonoBehaviour
{
    public static EnemyTargetSelectionManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private EnemySlotBoard enemySlotBoard;

    [Header("Selection mode hooks (在 Inspector 里指定进入/退出时要调用的函数)")]
    [SerializeField] private UnityEvent onEnterTargetSelectionMode = new UnityEvent();
    [SerializeField] private UnityEvent onExitTargetSelectionMode = new UnityEvent();

    [Header("Block UI while selecting (optional)")]
    [Tooltip("指定要禁止点击的 UI 根物体上的 CanvasGroup（整棵子树在选敌期间不可交互）。若该物体没有 CanvasGroup，运行时会自动添加一个。")]
    [SerializeField] private GameObject uiInputBlockRoot;

    private CanvasGroup uiInputBlockGroup;
    private bool uiInputBlockApplied;
    private bool savedUiInteractable;
    private bool savedUiBlocksRaycasts;

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

        if (onEnterTargetSelectionMode == null) onEnterTargetSelectionMode = new UnityEvent();
        if (onExitTargetSelectionMode == null) onExitTargetSelectionMode = new UnityEvent();

        EnsureUiInputBlockGroup();
    }

    private void EnsureUiInputBlockGroup()
    {
        if (uiInputBlockRoot == null) return;

        uiInputBlockGroup = uiInputBlockRoot.GetComponent<CanvasGroup>();
        if (uiInputBlockGroup == null)
            uiInputBlockGroup = uiInputBlockRoot.AddComponent<CanvasGroup>();
    }

    private void ApplyUiInputBlock()
    {
        EnsureUiInputBlockGroup();
        if (uiInputBlockGroup == null || uiInputBlockApplied) return;

        savedUiInteractable = uiInputBlockGroup.interactable;
        savedUiBlocksRaycasts = uiInputBlockGroup.blocksRaycasts;

        uiInputBlockGroup.interactable = false;
        uiInputBlockGroup.blocksRaycasts = false;

        uiInputBlockApplied = true;
    }

    private void RestoreUiInputBlock()
    {
        if (!uiInputBlockApplied || uiInputBlockGroup == null)
        {
            uiInputBlockApplied = false;
            return;
        }

        uiInputBlockGroup.interactable = savedUiInteractable;
        uiInputBlockGroup.blocksRaycasts = savedUiBlocksRaycasts;

        uiInputBlockApplied = false;
    }

    private void Update()
    {
        if (!isSelectingTarget) return;

        if (Input.GetMouseButtonDown(1))
            CancelSelection();
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

        ApplyUiInputBlock();

        onEnterTargetSelectionMode?.Invoke();

        Debug.Log("[EnemyTargetSelectionManager] Start selecting enemy target.");
    }

    /// <summary>
    /// 外部或右键取消可调用
    /// </summary>
    public void CancelSelection()
    {
        if (!isSelectingTarget) return;

        isSelectingTarget = false;
        onTargetSelected = null;

        RestoreUiInputBlock();

        onExitTargetSelectionMode?.Invoke();

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

        RestoreUiInputBlock();

        onExitTargetSelectionMode?.Invoke();

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

    public bool IsEnemySelectablePublic(EnemyUnit enemy)
    {
        return enemy != null && !enemy.IsDead && IsEnemySelectable(enemy);
    }

    public bool IsSelectingTarget()
    {
        return isSelectingTarget;
    }
}
