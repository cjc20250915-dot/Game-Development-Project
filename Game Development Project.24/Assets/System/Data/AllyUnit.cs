using System;
using System.Collections;
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

    [Header("Model Root")]
    [SerializeField] private Transform modelRoot;

    [Header("Hit Feedback")]
    [SerializeField] private float hitShakeDuration = 0.2f;
    [SerializeField] private float hitShakeDistance = 0.2f;
    [SerializeField] private int hitShakeVibrato = 3;

    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private int hitFlashCount = 2;

    [Header("Hit VFX / SFX")]
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private AudioClip hitSFX;

    [Header("Death VFX / SFX")]
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private AudioClip deathSFX;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Death")]
    [SerializeField] private float deathDelay = 0.4f;

    private bool deathStarted = false;

    private Coroutine hitFeedbackCoroutine;
    private Coroutine deathCoroutine;

    private Vector3 modelRootOriginalLocalPosition;
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        if (currentHP <= 0)
            currentHP = maxHP;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (modelRoot == null)
            modelRoot = transform;

        modelRootOriginalLocalPosition = modelRoot.localPosition;
        cachedRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || deathStarted) return;
        if (damage <= 0) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        OnHPChanged?.Invoke(currentHP, maxHP);

        Debug.Log($"{name} 收到了 {damage} 点伤害，剩余 {currentHP} 血量");

        PlayHitFeedback();

        if (currentHP == 0)
        {
            if (deathCoroutine != null)
                StopCoroutine(deathCoroutine);

            deathCoroutine = StartCoroutine(DieAfterDelay());
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || deathStarted) return;
        if (amount <= 0) return;

        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void PlayHitFeedback()
    {
        if (deathStarted) return;

        if (hitVFXPrefab != null)
            Instantiate(hitVFXPrefab, modelRoot.position, Quaternion.identity);

        if (audioSource != null && hitSFX != null)
            audioSource.PlayOneShot(hitSFX);

        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(hitFeedbackCoroutine);
            ResetHitVisualState();
        }

        hitFeedbackCoroutine = StartCoroutine(HitFeedbackRoutine());
    }

    private IEnumerator HitFeedbackRoutine()
    {
        float duration = Mathf.Max(hitShakeDuration, hitFlashDuration);
        float timer = 0f;

        int vibrato = Mathf.Max(1, hitShakeVibrato);
        int flashCount = Mathf.Max(1, hitFlashCount);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (timer <= hitShakeDuration)
            {
                float normalized = timer / Mathf.Max(0.0001f, hitShakeDuration);
                float damper = 1f - normalized;
                float wave = Mathf.Sin(normalized * Mathf.PI * vibrato);
                float offsetX = wave * hitShakeDistance * damper;

                modelRoot.localPosition = modelRootOriginalLocalPosition + new Vector3(offsetX, 0f, 0f);
            }

            if (timer <= hitFlashDuration)
            {
                float normalizedFlash = timer / Mathf.Max(0.0001f, hitFlashDuration);
                float flashWave = Mathf.Sin(normalizedFlash * Mathf.PI * flashCount * 2f);
                bool visible = flashWave >= 0f;

                SetRenderersVisible(visible);
            }

            yield return null;
        }

        ResetHitVisualState();
        hitFeedbackCoroutine = null;
    }

    private IEnumerator DieAfterDelay()
    {
        if (deathStarted) yield break;
        deathStarted = true;

        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(hitFeedbackCoroutine);
            hitFeedbackCoroutine = null;
        }

        ResetHitVisualState();

        if (deathVFXPrefab != null)
            Instantiate(deathVFXPrefab, modelRoot.position, Quaternion.identity);

        if (audioSource != null && deathSFX != null)
            audioSource.PlayOneShot(deathSFX);

        yield return new WaitForSeconds(deathDelay);

        Die();
    }

    private void Die()
    {
        Debug.Log($"[AllyUnit] {displayName} died.");
        OnDead?.Invoke(this);
    }

    private void ResetHitVisualState()
    {
        if (modelRoot != null)
            modelRoot.localPosition = modelRootOriginalLocalPosition;

        SetRenderersVisible(true);
    }

    private void SetRenderersVisible(bool visible)
    {
        if (cachedRenderers == null) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
                cachedRenderers[i].enabled = visible;
        }
    }
}