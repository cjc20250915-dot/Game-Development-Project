using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class TurnBattleManager : MonoBehaviour
{
    public enum Turn { Player, Enemy }

    [Header("Turn Settings")]
    public int movesPerTurn = 5;

    [Header("UI (Display)")]
    public TMP_Text movesText;

    [Header("Move UI Effect")]
public bool enableMoveUIEffect = true;
public float moveTextPunchScale = 0.25f;
public float moveTextPunchDuration = 0.25f;
public Color moveGainColor = Color.green;
public Color moveLoseColor = Color.yellow;

private Color movesTextOriginalColor = Color.white;
private Tween moveTextColorTween;
private Tween moveTextScaleTween;

    [Header("Turn Panel")]
    public ToggleSlideUI turnPanelUI;

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

    public event Action OnPlayerTurnBegan;
    public event Action OnPlayerTurnEnded;
    public event Action OnEnemyTurnBegan;
    public event Action OnEnemyTurnEnded;
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
    if (movesText != null)
        movesTextOriginalColor = movesText.color;

    BeginPlayerTurn();
}

    private void RefreshUI()
    {
        if (movesText != null)
            movesText.text = $"Moves: {remainingMoves}";
    }

    private void PlayMoveUIEffect(int delta)
{
    if (!enableMoveUIEffect || movesText == null) return;

    Transform textTf = movesText.transform;

    moveTextScaleTween?.Kill();
    moveTextColorTween?.Kill();

    textTf.localScale = Vector3.one;
    movesText.color = movesTextOriginalColor;

    // 跳动
    moveTextScaleTween = textTf.DOPunchScale(
        Vector3.one * moveTextPunchScale,
        moveTextPunchDuration,
        8,
        0.8f
    );

    // 闪光
    Color flashColor = delta >= 0 ? moveGainColor : moveLoseColor;

    moveTextColorTween = DOTween.Sequence()
        .Append(movesText.DOColor(flashColor, 0.08f))
        .Append(movesText.DOColor(movesTextOriginalColor, 0.18f));
}

private void RefreshUIWithEffect(int delta)
{
    RefreshUI();
    PlayMoveUIEffect(delta);
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

        moveCapThisTurn = remainingMoves;

        if (board != null)
        {
            board.SetBoardInputEnabled(true);
            board.SetBoardAlpha(1f);
        }

        SetLockedUIInteractable(true);

        RefreshUI();

        // 切回玩家回合：只切面板和文本，不动摄像机
if (turnPanelUI != null)
{
    turnPanelUI.SlideOnlyToA();
}

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

        if (board != null)
        {
            board.SetBoardInputEnabled(false);
            board.SetBoardAlpha(disabledBoardAlpha);
        }

        SetLockedUIInteractable(false);

        RefreshUI();

        // 切到敌方回合：只切面板和文本，不动摄像机
if (turnPanelUI != null)
{
    turnPanelUI.SlideOnlyToB();
}

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

    public bool TryConsumePlayerMove()
    {
        if (!IsPlayerTurn) return false;
        if (remainingMoves <= 0) return false;

remainingMoves--;
RefreshUIWithEffect(-1);

        Debug.Log($"[Turn] Player used 1 move. Remaining={remainingMoves}");
        return true;
    }

public void RestoreMoves(int amount)
{
    if (amount <= 0) return;

    remainingMoves += amount;

    if (remainingMoves > moveCapThisTurn)
        remainingMoves = moveCapThisTurn;

    if (IsPlayerTurn && remainingMoves > 0)
    {
        if (board != null)
        {
            board.SetBoardInputEnabled(true);
            board.SetBoardAlpha(1f);
        }
    }

    RefreshUIWithEffect(amount);

    Debug.Log($"[Turn] Restored {amount} move(s). Remaining={remainingMoves}/{moveCapThisTurn}");
}

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