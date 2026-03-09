using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("References")]
    public EnemySlotBoard enemyBoard;

    private List<EnemyUnit> enemies = new List<EnemyUnit>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupBattleFromNodeData();
    }

    void SetupBattleFromNodeData()
    {
        if (enemyBoard == null)
        {
            Debug.LogError("BattleManager: enemyBoard 没有赋值。");
            return;
        }

        if (GameRunManager.Instance == null)
        {
            Debug.LogError("BattleManager: GameRunManager 不存在。");
            return;
        }

        NodeData currentNode = GameRunManager.Instance.currentNode;

        if (currentNode == null)
        {
            Debug.LogError("BattleManager: currentNode 为空。");
            return;
        }

        enemyBoard.ApplyNodeData(currentNode);
        enemyBoard.SpawnAllEnemiesForBattle();

        enemies = new List<EnemyUnit>(enemyBoard.Enemies);

        Debug.Log("Battle Started. Node = " + currentNode.nodeName + " Enemy Count = " + enemies.Count);
    }

    public List<EnemyUnit> GetAliveEnemies()
    {
        List<EnemyUnit> alive = new List<EnemyUnit>();

        foreach (var e in enemies)
        {
            if (e != null && !e.IsDead)
            {
                alive.Add(e);
            }
        }

        return alive;
    }

    public bool AreAllEnemiesDead()
    {
        foreach (var e in enemies)
        {
            if (e != null && !e.IsDead)
            {
                return false;
            }
        }

        return true;
    }

    void Update()
    {
        if (enemies.Count > 0 && AreAllEnemiesDead())
        {
            Debug.Log("Battle Win!");
            EndBattle();
        }
    }

    void EndBattle()
    {
        Debug.Log("Battle Finished");
        // 这里以后可以加奖励、返回地图等逻辑
    }
}