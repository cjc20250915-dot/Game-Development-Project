using UnityEngine;

public class EnemyHPBoard : MonoBehaviour
{
    public HealthBarUI[] hpSlots;
    public EnemySlotBoard enemyBoard;

    void Start()
    {
        Bind();
    }

    void Bind()
    {
        var enemies = enemyBoard.Enemies;

        for (int i = 0; i < hpSlots.Length; i++)
        {
            if (i < enemies.Count)
            {
                hpSlots[i].BindEnemy(enemies[i]);
                hpSlots[i].gameObject.SetActive(true);
            }
            else
            {
                hpSlots[i].gameObject.SetActive(false);
            }
        }
    }
}