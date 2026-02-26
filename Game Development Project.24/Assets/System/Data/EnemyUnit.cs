using System;
using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 30;
    public int currentHP;

    public bool IsDead => currentHP <= 0;

    /// <summary>
    /// 当血量变化时触发（用于刷新血条UI）
    /// </summary>
    public event Action<int, int> OnHPChanged; // currentHP, maxHP

    /// <summary>
    /// 当敌人死亡时触发
    /// </summary>
    public event Action OnDead;

    private void Awake()
    {
        currentHP = maxHP;
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(0, currentHP);

        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        currentHP += amount;
        currentHP = Mathf.Min(maxHP, currentHP);

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void Die()
    {
        OnDead?.Invoke();
        Debug.Log("[Enemy] Dead");
    }
}