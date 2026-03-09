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
        StartBattle();
    }

    void StartBattle()
    {
        if (enemyBoard == null)
        {
            Debug.LogError("EnemySlotBoard not assigned!");
            return;
        }

        enemies = new List<EnemyUnit>(enemyBoard.Enemies);

        Debug.Log("Battle Started. Enemy count: " + enemies.Count);
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
        if (AreAllEnemiesDead())
        {
            Debug.Log("Battle Win!");

            // 战斗结束
            EndBattle();
        }
    }

    void EndBattle()
    {
        // 这里以后可以加奖励 / 返回地图
        Debug.Log("Battle Finished");
    }
}