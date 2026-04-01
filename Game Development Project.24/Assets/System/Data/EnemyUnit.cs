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
    [SerializeField] private float deathDelay = 0.4f;

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

    private bool deathStarted = false;

    private Coroutine hitFeedbackCoroutine;
    private Coroutine deathCoroutine;

    private Vector3 modelRootOriginalLocalPosition;
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        currentHP = maxHP;

        if (modelRoot == null)
            modelRoot = transform;

        modelRootOriginalLocalPosition = modelRoot.localPosition;
        cachedRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || deathStarted) return;

        currentHP -= damage;
        currentHP = Mathf.Max(0, currentHP);

        OnHPChanged?.Invoke(currentHP, maxHP);

        PlayHitFeedback();

        if (currentHP <= 0)
        {
            if (deathCoroutine != null)
                StopCoroutine(deathCoroutine);

            deathCoroutine = StartCoroutine(DieAfterDelay());
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || deathStarted) return;

        currentHP += amount;
        currentHP = Mathf.Min(maxHP, currentHP);

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

            // 左右摆动：只作用在 modelRoot
            if (timer <= hitShakeDuration)
            {
                float normalized = timer / Mathf.Max(0.0001f, hitShakeDuration);
                float damper = 1f - normalized;
                float wave = Mathf.Sin(normalized * Mathf.PI * vibrato);
                float offsetX = wave * hitShakeDistance * damper;

                modelRoot.localPosition = modelRootOriginalLocalPosition + new Vector3(offsetX, 0f, 0f);
            }

            // 闪动：也只作用在 modelRoot 下的渲染器
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
        OnDead?.Invoke();
        Debug.Log("[Enemy] Dead");
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