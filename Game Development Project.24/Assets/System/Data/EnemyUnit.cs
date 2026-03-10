using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 30;
    public int currentHP;

    public bool IsDead => currentHP <= 0;

    [Header("Death")]
    [SerializeField] private float deathDelay = 0.5f;

    /// <summary>
    /// 当血量变化时触发（用于刷新血条UI）
    /// </summary>
    public event Action<int, int> OnHPChanged; // currentHP, maxHP

    /// <summary>
    /// 当敌人死亡时触发
    /// </summary>
    public event Action OnDead;

    [Header("Combat Stats")]
    public int attackPower = 5;
    public int speed = 1;
    public int actionsPerTurn = 1;

    [Header("AI Action Probability")]
    [Range(0f, 1f)] public float probAttack = 0.7f;
    [Range(0f, 1f)] public float probSkill = 0.2f;
    [Range(0f, 1f)] public float probDefend = 0.1f;

    [Header("Skills")]
    public List<SkillData> skills = new List<SkillData>();

    private bool deathStarted = false;

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
            StartCoroutine(DieAfterDelay());
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        currentHP += amount;
        currentHP = Mathf.Min(maxHP, currentHP);

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private IEnumerator DieAfterDelay()
    {
        if (deathStarted) yield break;
        deathStarted = true;

        yield return new WaitForSeconds(deathDelay);

        Die();
    }

    private void Die()
    {
        OnDead?.Invoke();
        Debug.Log("[Enemy] Dead");
    }
}