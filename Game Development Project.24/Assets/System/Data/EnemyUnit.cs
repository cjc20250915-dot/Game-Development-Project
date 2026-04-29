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
    [Tooltip("HP 归零后，延迟这么久再派发 OnDead")]
    public float deathDelay = 1.5f;

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
    [Tooltip("受伤时在模型位置生成的特效")]
    public GameObject hitVFXPrefab;
    [Tooltip("受伤时播放的音效")]
    public AudioClip hitSFX;

    [Header("Death VFX / SFX")]
    [Tooltip("死亡流程开始时生成的特效")]
    public GameObject deathVFXPrefab;
    [Tooltip("死亡流程开始时播放的音效")]
    public AudioClip deathSFX;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private bool deathStarted = false;

    private Coroutine hitFeedbackCoroutine;
    private Coroutine deathCoroutine;

    private Vector3 modelRootOriginalLocalPosition;
    private Renderer[] cachedRenderers;

    [Header("Defend")]
[SerializeField] private bool isDefending = false;
[SerializeField] private float defendDamageMultiplier = 0.5f;

public bool IsDefending => isDefending;

public void EnterDefend()
{
    if (IsDead || deathStarted) return;
    isDefending = true;
}

public void ClearDefend()
{
    isDefending = false;
}

    [Header("Freeze (player skill)")]
    [Tooltip("由技能施加：下一次敌方回合开始时若仍为 true，则本回合跳过行动（Consume 后清除）。敌方回合开始的防御清除仍会执行。")]
    [SerializeField] private bool freezeSkipNextEnemyPhase;

    [Header("Freeze Visual")]
    [Tooltip("冰冻观感：追加到 modelRoot 下所有 Renderer 的材质槽末尾。建议使用 Assets/Shader 下的冰冻材质（如 IceEffectMat）。")]
    public Material freezeOverlayMaterial;

    private Material[][] freezeMaterialBackup;
    private bool freezeOverlayApplied;
    private GameObject freezeVFXInstance;

    /// <summary>是否已成功挂上冻结标记（用于技能音效：避免击杀后仍播冻结音）。</summary>
    public bool HasFreezeScheduledForNextEnemyPhase => freezeSkipNextEnemyPhase;

    /// <summary>标记：下一次敌方行动中跳过全部行动（攻击/技能/防御均不执行）。</summary>
    public void ScheduleFreezeNextEnemyPhase(GameObject freezeVFXPrefab = null)
    {
        if (IsDead || deathStarted)
            return;

        freezeSkipNextEnemyPhase = true;
        ApplyFreezeVisualOverlay();
        ApplyFreezeVFX(freezeVFXPrefab);
    }

    /// <summary>若本敌人因冻结应跳过本回合行动，返回 true 并清除冻结标记。</summary>
    public bool ConsumeFreezeSkipActionsIfScheduled()
    {
        if (!freezeSkipNextEnemyPhase)
            return false;

        freezeSkipNextEnemyPhase = false;
        RemoveFreezeVisualOverlay();
        RemoveFreezeVFX();
        return true;
    }

    private void ApplyFreezeVisualOverlay()
    {
        if (freezeOverlayMaterial == null || freezeOverlayApplied || modelRoot == null)
            return;

        Renderer[] rends = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0)
            return;

        freezeMaterialBackup = new Material[rends.Length][];

        for (int i = 0; i < rends.Length; i++)
        {
            Renderer r = rends[i];
            if (r == null)
                continue;

            freezeMaterialBackup[i] = r.sharedMaterials;

            var mats = new Material[r.sharedMaterials.Length + 1];
            r.sharedMaterials.CopyTo(mats, 0);
            mats[mats.Length - 1] = freezeOverlayMaterial;
            r.materials = mats;
        }

        freezeOverlayApplied = true;
    }

    private void RemoveFreezeVisualOverlay()
    {
        if (!freezeOverlayApplied || freezeMaterialBackup == null || modelRoot == null)
            return;

        Renderer[] rends = modelRoot.GetComponentsInChildren<Renderer>(true);
        int n = Mathf.Min(rends.Length, freezeMaterialBackup.Length);

        for (int i = 0; i < n; i++)
        {
            Renderer r = rends[i];
            if (r != null && freezeMaterialBackup[i] != null)
                r.materials = freezeMaterialBackup[i];
        }

        freezeMaterialBackup = null;
        freezeOverlayApplied = false;
    }

    private void ApplyFreezeVFX(GameObject freezeVFXPrefab)
    {
        if (freezeVFXPrefab == null || freezeVFXInstance != null)
            return;

        Transform parent = modelRoot != null ? modelRoot : transform;
        freezeVFXInstance = Instantiate(freezeVFXPrefab, parent.position, Quaternion.identity, parent);
        freezeVFXInstance.transform.localPosition = Vector3.zero;
    }

    private void RemoveFreezeVFX()
    {
        if (freezeVFXInstance == null)
            return;

        Destroy(freezeVFXInstance);
        freezeVFXInstance = null;
    }

    private void Awake()
    {
        currentHP = maxHP;

        if (modelRoot == null)
            modelRoot = transform;

        modelRootOriginalLocalPosition = modelRoot.localPosition;
        cachedRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void Start()
    {
        // 槽位系统会在 Instantiate 后再设置 local pose，这里二次采样避免受击重置到旧坐标。
        if (modelRoot != null)
            modelRootOriginalLocalPosition = modelRoot.localPosition;
    }

public void TakeDamage(int damage)
{
    if (IsDead || deathStarted) return;

    int finalDamage = damage;

    if (isDefending)
        finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * defendDamageMultiplier));

    currentHP -= finalDamage;
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
        freezeSkipNextEnemyPhase = false;
        RemoveFreezeVisualOverlay();
        RemoveFreezeVFX();
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