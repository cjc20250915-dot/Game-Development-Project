using System;
using System.Collections.Generic;
using UnityEngine;

public class AllyUnit : MonoBehaviour
{
    [Header("Basic Info")]
    public string displayName = "Ally";

    [Header("HP")]
    [Min(1)] public int maxHP = 30;
    public int currentHP;

    [Header("Turn Contribution")]
    [Tooltip("该角色每回合提供的步数")]
    [Min(0)] public int stepsPerTurn = 2;

    [Header("Skills")]
    [Tooltip("该角色可用的技能列表")]
    public List<SkillData> skills = new List<SkillData>();

    public bool IsDead => currentHP <= 0;

    /// <summary>
    /// 血量变化事件（用于刷新血条UI）
    /// </summary>
    public event Action<int, int> OnHPChanged; // currentHP, maxHP

    /// <summary>
    /// 角色死亡事件（用于通知队伍系统 / 回合系统）
    /// </summary>
    public event Action<AllyUnit> OnDead;

    private void Awake()
    {
        if (currentHP <= 0)
            currentHP = maxHP;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        if (damage <= 0) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP == 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void Die()
    {
        Debug.Log($"[AllyUnit] {displayName} died.");
        OnDead?.Invoke(this);
    }
}