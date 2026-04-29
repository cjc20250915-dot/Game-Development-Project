using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyUnit : MonoBehaviour
{
    [Header("Basic Info")]
    public string displayName = "Ally";

    [Tooltip("战斗 HUD、头像框等使用的角色图片（Sprite）")]
    public Sprite characterPortrait;

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
    [Tooltip("受伤时在模型位置生成的特效")]
    public GameObject hitVFXPrefab;
    [Tooltip("受伤时播放的音效")]
    public AudioClip hitSFX;

    [Header("Heal VFX / SFX")]
    [Tooltip("回血时在 modelRoot 世界坐标生成的特效；粒子请勿勾选 Looping（持续发射），否则会约 3 秒后强制销毁实例")]
    public GameObject healVFXPrefab;
    [Tooltip("回血时播放的音效（通过 AllyUnit.Heal 触发）")]
    public AudioClip healSFX;

    [Header("Death VFX / SFX")]
    [Tooltip("死亡流程开始时生成的特效")]
    public GameObject deathVFXPrefab;
    [Tooltip("死亡流程开始时播放的音效")]
    public AudioClip deathSFX;

    [Header("Audio")]
    [Tooltip("可为空：运行时自动在本物体或子物体上查找 AudioSource")]
    [SerializeField] private AudioSource audioSource;

    [Header("Death")]
    [Tooltip("HP 归零后，延迟这么久再派发 OnDead（与 EnemyUnit 一致，便于播放死亡表现）")]
    public float deathDelay = 1.5f;

    private bool deathStarted = false;

    private Coroutine hitFeedbackCoroutine;
    private Coroutine deathCoroutine;

    private Vector3 modelRootOriginalLocalPosition;
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        EnsureAudioSource();

        if (currentHP <= 0)
            currentHP = maxHP;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (modelRoot == null)
            modelRoot = transform;

        modelRootOriginalLocalPosition = modelRoot.localPosition;
        cachedRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);
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

    /// <param name="amount">回复量</param>
    /// <param name="healVFXOverride">非空则代替预制体上的 healVFXPrefab（技能回血等）</param>
    /// <param name="healSFXOverride">非空则代替预制体上的 healSFX</param>
    public void Heal(int amount, GameObject healVFXOverride = null, AudioClip healSFXOverride = null)
    {
        if (IsDead || deathStarted) return;
        if (amount <= 0) return;

        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;

        OnHPChanged?.Invoke(currentHP, maxHP);

        EnsureAudioSource();

        GameObject vfxPrefab = healVFXOverride != null ? healVFXOverride : healVFXPrefab;
        if (vfxPrefab != null)
            SpawnHealVfx(vfxPrefab);

        AudioClip sfx = healSFXOverride != null ? healSFXOverride : healSFX;
        if (audioSource != null && sfx != null)
            audioSource.PlayOneShot(sfx);
    }

    /// <summary>
    /// 生成在友方 <see cref="modelRoot"/> 的世界坐标处；带 Cartoon FX 的预制体会自行销毁，
    /// 否则按粒子时长兜底销毁（避免 Looping 粒子永远不删导致「一直在播」）。
    /// </summary>
    private void SpawnHealVfx(GameObject prefab)
    {
        GameObject vfx = Instantiate(prefab, modelRoot.position, Quaternion.identity);

        // 与 VFXManager：CFXR 自带结束销毁；否则 Destroy 兜底
        if (vfx.GetComponentInChildren<CartoonFX.CFXR_Effect>(true) != null)
            return;

        Destroy(vfx, EstimateHealVfxDestroyDelay(vfx));
    }

    /// <summary>
    /// 根据非 Loop 粒子的 duration + lifetime 估算；若存在 Looping 或未检测到粒子则短延时销毁。
    /// </summary>
    private static float EstimateHealVfxDestroyDelay(GameObject root)
    {
        float maxEnd = 0f;
        bool anyLooping = false;

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            if (main.loop)
            {
                anyLooping = true;
                continue;
            }

            float startLife = main.startLifetime.constant;
            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                startLife = Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);

            float end = main.duration + startLife;
            if (end > maxEnd)
                maxEnd = end;
        }

        if (anyLooping && maxEnd <= 0f)
            return 3f;

        if (maxEnd <= 0f)
            return 3f;

        return Mathf.Clamp(maxEnd + 0.25f, 0.75f, 20f);
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

    private void OnDisable()
    {
        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(hitFeedbackCoroutine);
            hitFeedbackCoroutine = null;
        }

        // 兜底：避免闪烁协程中断后模型保持隐藏。
        ResetHitVisualState();
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