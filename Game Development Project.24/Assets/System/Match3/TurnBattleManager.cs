using System;
using UnityEngine;
using TMPro;

public class TurnBattleManager : MonoBehaviour
{
    public enum Turn { Player, Enemy }

    [Header("Turn Settings")]
    public int movesPerTurn = 5;

    [Header("UI (Display)")]
    public TMP_Text movesText;

    [Header("Ally Slots")]
    public AllySlotBoard allySlots;

    [Header("UI Lock (Disable a specific UI subtree)")]
    [Tooltip("敌人回合时要禁用操作的UI根节点（它及子物体都会被禁用Raycast/交互）。不要把全局UI都放这里面。")]
    public GameObject uiRootToDisableOnEnemyTurn;

    [Header("Board Visual")]
    [SerializeField] private float disabledBoardAlpha = 0.4f;

    [Header("Runtime (Read Only)")]
    [SerializeField] private Turn currentTurn = Turn.Player;
    [SerializeField] private int remainingMoves;
    [SerializeField] private int moveCapThisTurn;

    private BoardUIManager board;
    private CanvasGroup uiLockGroup;

    public bool IsPlayerTurn => currentTurn == Turn.Player;
    public int RemainingMoves => remainingMoves;
    public int MoveCapThisTurn => moveCapThisTurn;

    // ===== Turn events =====
    public event Action OnPlayerTurnBegan;
    public event Action OnPlayerTurnEnded;
    public event Action OnEnemyTurnBegan;
    public event Action OnEnemyTurnEnded;

    // 敌人AI开始行动：空钩子（敌人AI在别处订阅/调用）
    public event Action OnEnemyAIRequested;

    private void Awake()
    {
        board = FindFirstObjectByType<BoardUIManager>();

        if (allySlots == null)
            allySlots = FindFirstObjectByType<AllySlotBoard>();

        if (uiRootToDisableOnEnemyTurn != null)
        {
            uiLockGroup = uiRootToDisableOnEnemyTurn.GetComponent<CanvasGroup>();
            if (uiLockGroup == null)
                uiLockGroup = uiRootToDisableOnEnemyTurn.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        BeginPlayerTurn();
    }

    private void RefreshUI()
    {
        if (movesText != null)
            movesText.text = $"Moves: {remainingMoves}";
    }

    public void OnEndTurnButtonClicked()
    {
        if (!IsPlayerTurn) return;
        BeginEnemyTurn();
    }

    public void TestReturnToPlayerTurn()
    {
        if (!IsPlayerTurn)
        {
            BeginPlayerTurn();
        }
    }

    public void BeginPlayerTurn()
    {
        currentTurn = Turn.Player;

        int fromAllies = (allySlots != null) ? allySlots.TotalStepsPerTurn : 0;
        remainingMoves = Mathf.Max(1, fromAllies);

        // 本回合开始时记录“本回合步数上限”
        moveCapThisTurn = remainingMoves;

        // 玩家回合：允许棋盘操作 + 恢复显示
        if (board != null)
        {
            board.SetBoardInputEnabled(true);
            board.SetBoardAlpha(1f);
        }

        // 玩家回合：恢复那一部分UI的操作
        SetLockedUIInteractable(true);

        RefreshUI();
        OnPlayerTurnBegan?.Invoke();

        Debug.Log($"[Turn] Player turn began. Moves={remainingMoves}, Cap={moveCapThisTurn}");
    }

    public void EndPlayerTurn()
    {
        if (currentTurn != Turn.Player) return;

        OnPlayerTurnEnded?.Invoke();
        Debug.Log("[Turn] Player turn ended.");
    }

    public void BeginEnemyTurn()
    {
        currentTurn = Turn.Enemy;

        // 敌人回合：禁用棋盘操作 + 半透明
        if (board != null)
        {
            board.SetBoardInputEnabled(false);
            board.SetBoardAlpha(disabledBoardAlpha);
        }

        // 敌人回合：禁用你指定的那一块UI
        SetLockedUIInteractable(false);

        RefreshUI();
        OnEnemyTurnBegan?.Invoke();

        Debug.Log("[Turn] Enemy turn began.");

        RequestEnemyAIStart();
    }

    public void EndEnemyTurn()
    {
        if (currentTurn != Turn.Enemy) return;

        OnEnemyTurnEnded?.Invoke();
        Debug.Log("[Turn] Enemy turn ended.");
    }

    private void RequestEnemyAIStart()
    {
        Debug.Log("[Enemy] AI requested. (Implement AI elsewhere)");
        OnEnemyAIRequested?.Invoke();
    }

    private void SetLockedUIInteractable(bool enabled)
    {
        if (uiLockGroup == null) return;
        uiLockGroup.interactable = enabled;
        uiLockGroup.blocksRaycasts = enabled;
    }

    /// <summary>
    /// 每次玩家确认一次交换就消耗一步
    /// </summary>
    public bool TryConsumePlayerMove()
    {
        if (!IsPlayerTurn) return false;
        if (remainingMoves <= 0) return false;

        remainingMoves--;
        RefreshUI();

        Debug.Log($"[Turn] Player used 1 move. Remaining={remainingMoves}");
        return true;
    }

    public void RestoreMoves(int amount)
    {
        if (amount <= 0) return;

        remainingMoves += amount;

        if (remainingMoves > moveCapThisTurn)
            remainingMoves = moveCapThisTurn;

        // 如果当前还是玩家回合，并且步数恢复到了可用状态
        if (IsPlayerTurn && remainingMoves > 0)
        {
            if (board != null)
            {
                board.SetBoardInputEnabled(true);
                board.SetBoardAlpha(1f);
            }
        }

        RefreshUI();

        Debug.Log($"[Turn] Restored {amount} move(s). Remaining={remainingMoves}/{moveCapThisTurn}");
    }

    /// <summary>
    /// 由 BoardUIManager 在 resolve 完全结束时调用
    /// 现在不再自动切敌人回合
    /// </summary>
    public void OnBoardResolveFinished()
    {
        if (IsPlayerTurn && remainingMoves <= 0)
        {
            if (board != null)
            {
                board.SetBoardInputEnabled(false);
                board.SetBoardAlpha(disabledBoardAlpha);
            }

            Debug.Log("[Turn] Player moves depleted. Board input disabled. (No auto switch)");
        }
    }
}