using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnBattleManager : MonoBehaviour
{
    public enum Turn { Player, Enemy }

    [Header("Turn Settings")]
    public int movesPerTurn = 5;
    public float enemyTurnDelay = 0.3f;
    public float enemyActionTime = 0.8f;

    [Header("UI")]
    public TMP_Text movesText;              
    public GameObject enemyActingUI;    // “敌人在行动”面板/组件

    [Header("Runtime (Read Only)")]
    [SerializeField] private Turn currentTurn = Turn.Player;
    [SerializeField] private int remainingMoves;

    private BoardUIManager board;

    public bool IsPlayerTurn => currentTurn == Turn.Player;
    public int RemainingMoves => remainingMoves;

    private void Awake()
    {
        board = FindFirstObjectByType<BoardUIManager>();
    }

    private void Start()
    {
        StartPlayerTurn();
    }

    private void RefreshUI()
    {
        if (movesText != null)
            movesText.text = $"Moves: {remainingMoves}"; // 你想中文就改成 “步数：{remainingMoves}”

        if (enemyActingUI != null)
            enemyActingUI.SetActive(currentTurn == Turn.Enemy);
    }

    public void StartPlayerTurn()
    {
        currentTurn = Turn.Player;
        remainingMoves = Mathf.Max(1, movesPerTurn);

        // 玩家回合：允许操作
        if (board != null) board.SetBoardInputEnabled(true);

        RefreshUI();
        Debug.Log($"[Turn] Player turn start. Moves={remainingMoves}");
    }

    public void StartEnemyTurn()
    {
        if (currentTurn == Turn.Enemy) return;

        currentTurn = Turn.Enemy;

        // 敌人回合：禁用操作 + 显示UI
        if (board != null) board.SetBoardInputEnabled(false);

        RefreshUI();
        Debug.Log("[Turn] Enemy turn start.");

        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(enemyTurnDelay);

        // TODO: 敌人行动逻辑（先Debug）
        Debug.Log("[Enemy] Acting... (TODO)");

        yield return new WaitForSeconds(enemyActionTime);

        Debug.Log("[Turn] Enemy turn end.");
        StartPlayerTurn();
    }

    /// <summary>每次玩家确认一次交换就消耗一步</summary>
    public bool TryConsumePlayerMove()
    {
        if (!IsPlayerTurn) return false;
        if (remainingMoves <= 0) return false;

        remainingMoves--;
        RefreshUI();

        Debug.Log($"[Turn] Player used 1 move. Remaining={remainingMoves}");
        return true;
    }

    /// <summary>由 BoardUIManager 在 resolve 完全结束时调用</summary>
    public void OnBoardResolveFinished()
    {
        // 玩家回合且步数用尽 -> 切敌人回合
        if (IsPlayerTurn && remainingMoves <= 0)
            StartEnemyTurn();
    }
}