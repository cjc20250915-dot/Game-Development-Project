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

    [Header("Runtime (Read Only)")]
    [SerializeField] private Turn currentTurn = Turn.Player;
    [SerializeField] private int remainingMoves;
    [SerializeField] private int moveCapThisTurn;

    private BoardUIManager board;
    private CanvasGroup uiLockGroup;

    public bool IsPlayerTurn => currentTurn == Turn.Player;
    public int RemainingMoves => remainingMoves;

    // ===== Turn events (你要的“分出来的回合相关事件”) =====
    public event Action OnPlayerTurnBegan;
    public event Action OnPlayerTurnEnded;
    public event Action OnEnemyTurnBegan;
    public event Action OnEnemyTurnEnded;

    public int MoveCapThisTurn => moveCapThisTurn;

    // 敌人AI开始行动：空钩子（敌人AI在别处订阅/调用）
    public event Action OnEnemyAIRequested;

    private void Awake()
    {
        board = FindFirstObjectByType<BoardUIManager>();
        if (allySlots == null)
    allySlots = FindFirstObjectByType<AllySlotBoard>();

        // 为需要禁用的UI根节点准备 CanvasGroup（用于只禁用这一块UI的交互，不影响其他UI）
        if (uiRootToDisableOnEnemyTurn != null)
        {
            uiLockGroup = uiRootToDisableOnEnemyTurn.GetComponent<CanvasGroup>();
            if (uiLockGroup == null) uiLockGroup = uiRootToDisableOnEnemyTurn.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // 你也可以不自动开始，按你的流程在外部调用 BeginPlayerTurn()
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

    // 结束回合：直接切到敌人回合
    BeginEnemyTurn();
}
public void TestReturnToPlayerTurn()
{
    if (!IsPlayerTurn)
    {
        BeginPlayerTurn();
    }
}

    // ===== Core: only per-turn enter/exit (不负责“何时切换”，只负责“进入某回合时做什么”) =====

  public void BeginPlayerTurn()
{
    currentTurn = Turn.Player;

    int fromAllies = (allySlots != null) ? allySlots.TotalStepsPerTurn : 0;
    remainingMoves = Mathf.Max(1, fromAllies);

    // 本回合开始时记录“本回合步数上限”
    moveCapThisTurn = remainingMoves;

    // 玩家回合：允许棋盘操作
    if (board != null) board.SetBoardInputEnabled(true);

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

        // 敌人回合：禁用棋盘操作
        if (board != null) board.SetBoardInputEnabled(false);

        // 敌人回合：禁用你指定的那一块UI（只影响该根节点及子节点）
        SetLockedUIInteractable(false);

        RefreshUI();
        OnEnemyTurnBegan?.Invoke();

        Debug.Log("[Turn] Enemy turn began.");

        // 敌人AI开始行动（空逻辑钩子，AI在别处订阅这个事件）
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
        // 这里不写AI，只抛事件/钩子
        Debug.Log("[Enemy] AI requested. (Implement AI elsewhere)");
        OnEnemyAIRequested?.Invoke();
    }

    private void SetLockedUIInteractable(bool enabled)
    {
        if (uiLockGroup == null) return;
        uiLockGroup.interactable = enabled;
        uiLockGroup.blocksRaycasts = enabled;
        // 注意：不改 alpha，不影响显示；只禁用交互
    }

    /// <summary>每次玩家确认一次交换就消耗一步（你现在的“交换也消耗步数”逻辑会用到）</summary>
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

    RefreshUI();

    Debug.Log($"[Turn] Restored {amount} move(s). Remaining={remainingMoves}/{moveCapThisTurn}");
}

    /// <summary>
    /// 由 BoardUIManager 在 resolve 完全结束时调用
    /// 现在不再自动切敌人回合（你说“中间切换等下再写”）
    /// </summary>
    public void OnBoardResolveFinished()
    {
        // 这里只做“状态通知/检查”，不做切换
        if (IsPlayerTurn && remainingMoves <= 0)
        {
            // 你之前的需求：步数耗尽后禁用棋盘（不切回合）
            if (board != null) board.SetBoardInputEnabled(false);
            Debug.Log("[Turn] Player moves depleted. Board input disabled. (No auto switch)");
        }
    }
}