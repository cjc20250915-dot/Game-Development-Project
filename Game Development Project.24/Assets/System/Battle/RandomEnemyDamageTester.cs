using System.Collections.Generic;
using UnityEngine;

public class RandomEnemyDamageTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySlotBoard enemySlotBoard;

    [Header("Damage Settings")]
    [SerializeField] private int damage = 10;

    public void DealRandomDamage()
    {
        if (enemySlotBoard == null)
        {
            Debug.LogWarning("EnemySlotBoard 没有绑定。");
            return;
        }

        IReadOnlyList<EnemyUnit> enemies = enemySlotBoard.Enemies;

        if (enemies == null || enemies.Count == 0)
        {
            Debug.Log("当前没有敌人可攻击。");
            return;
        }

        List<EnemyUnit> validEnemies = new List<EnemyUnit>();

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null) continue;

            // 只攻击还活着的敌人
            if (!enemy.IsDead)
            {
                validEnemies.Add(enemy);
            }
        }

        if (validEnemies.Count == 0)
        {
            Debug.Log("当前没有存活敌人可攻击。");
            return;
        }

        int randomIndex = Random.Range(0, validEnemies.Count);
        EnemyUnit target = validEnemies[randomIndex];

        Debug.Log($"随机攻击敌人：{target.name}，造成 {damage} 点伤害");

        target.TakeDamage(damage);
    }
}