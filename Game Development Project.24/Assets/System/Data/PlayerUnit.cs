using System;
using UnityEngine;

public class PlayerUnit : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 50;
    public int currentHP;

    public bool IsDead => currentHP <= 0;

    /// <summary>
    /// 血量变化事件（UI监听）
    /// </summary>
    public event Action<int, int> OnHPChanged; // currentHP, maxHP

    /// <summary>
    /// 玩家死亡事件（战败）
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
        Debug.Log("[Player] Dead");
    }
}