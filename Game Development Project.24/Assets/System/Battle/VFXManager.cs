using System.Collections;
using UnityEngine;

// ============================================================
//  VFXManager — 特效播放管理器（挂在场景中的单例对象上）
//  使用方式：VFXManager.Instance.Play_XXX(position / target)
// ============================================================

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("配表资产")]
    [SerializeField] private VFXConfig config;

    // ── Shader Property ID（提前缓存，避免字符串开销）──────────
    private int dissolvePropertyID;
    private int lightningIntensityID;   // 镭射扫描强度（根据你的 Shader 修改属性名）

    // ─────────────────────────────────────────────────────────
    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (config != null)
        {
            dissolvePropertyID   = Shader.PropertyToID(config.dissolvePropertyName);
            lightningIntensityID = Shader.PropertyToID("_ScanIntensity"); // 按你的 Shader 改
        }
    }
    #endregion

    // ─────────────────────────────────────────────────────────
    #region 公共播放接口

    /// <summary>我方恢复生命值特效</summary>
    public void PlayHeal(Vector3 position)
    {
        SpawnVFX(config.healVFX, position);
    }

    /// <summary>普通单体攻击</summary>
    public void PlayNormalAttack(Vector3 position)
    {
        SpawnVFX(config.normalAttackVFX, position);
    }

    /// <summary>电属性单体攻击 + 镭射扫描 Shader</summary>
    /// <param name="target">目标对象，用于叠加 Shader 效果</param>
    public void PlayLightningAttack(Vector3 position, GameObject target = null)
    {
        SpawnVFX(config.lightningAttackVFX, position);

        if (target != null && config.lightningHitShaderMaterial != null)
            StartCoroutine(ApplyLightningShader(target));
    }

    /// <summary>风属性范围攻击</summary>
    public void PlayWindAoE(Vector3 position)
    {
        SpawnVFX(config.windAoEVFX, position);
    }

    /// <summary>火属性范围攻击</summary>
    public void PlayFireAoE(Vector3 position)
    {
        SpawnVFX(config.fireAoEVFX, position);
    }

    /// <summary>冰属性单体攻击 + 冰冻状态 Shader</summary>
    /// <param name="target">目标对象，用于叠加冰冻效果</param>
    public void PlayIceAttack(Vector3 position, GameObject target = null)
    {
        SpawnVFX(config.iceAttackVFX, position);

        if (target != null && config.iceFrozenShaderMaterial != null)
            StartCoroutine(ApplyFrozenShader(target, config.iceFrozenDuration));
    }

    /// <summary>敌人护盾效果（持续显示，需手动调用 StopEnemyShield 关闭）</summary>
    /// <returns>返回生成的特效对象，供外部管理生命周期</returns>
    public GameObject PlayEnemyShield(Vector3 position, Transform parent = null)
    {
        if (config.enemyShieldVFX == null) return null;

        GameObject vfx = Instantiate(config.enemyShieldVFX, position, Quaternion.identity);
        if (parent != null) vfx.transform.SetParent(parent, worldPositionStays: true);
        return vfx;
    }

    /// <summary>停止并销毁护盾特效</summary>
    public void StopEnemyShield(ref GameObject shieldVFXInstance)
    {
        if (shieldVFXInstance != null)
        {
            // 停止粒子发射，让已发出的粒子自然消亡
            var ps = shieldVFXInstance.GetComponent<ParticleSystem>();
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            Destroy(shieldVFXInstance, 2f);
            shieldVFXInstance = null;
        }
    }

    /// <summary>敌人死亡 = 粒子特效 + 溶解 Shader</summary>
    /// <param name="target">敌人 GameObject，用于播放溶解动画</param>
    public void PlayEnemyDeath(Vector3 position, GameObject target = null)
    {
        SpawnVFX(config.enemyDeathVFX, position);

        if (target != null && config.enemyDeathDissolveShaderMaterial != null)
            StartCoroutine(ApplyDissolveShader(target, config.dissolveduration));
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region 内部工具

    /// <summary>生成一次性粒子特效，CFXR 会自动回收，这里再加 fallback 保险</summary>
    private void SpawnVFX(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[VFXManager] 特效 Prefab 未赋值！请检查 VFXConfig。");
            return;
        }

        GameObject vfx = Instantiate(prefab, position, Quaternion.identity);

        // CFXR_Effect 会自动销毁，这里加个安全兜底（3s）
        var cfxr = vfx.GetComponent<CartoonFX.CFXR_Effect>();
        if (cfxr == null)
            Destroy(vfx, 3f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Shader 协程

    /// <summary>
    /// 电属性镭射扫描 Shader：
    /// 将目标的所有 Renderer 换成镭射材质，短暂闪烁后还原
    /// </summary>
    private IEnumerator ApplyLightningShader(GameObject target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        var originalMaterials = new Material[renderers.Length][];

        // 备份原材质
        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterials;

        // 叠加镭射 Shader（追加到材质槽末位，不替换原有材质）
        for (int i = 0; i < renderers.Length; i++)
        {
            var mats = new Material[renderers[i].sharedMaterials.Length + 1];
            renderers[i].sharedMaterials.CopyTo(mats, 0);
            mats[mats.Length - 1] = config.lightningHitShaderMaterial;
            renderers[i].materials = mats;
        }

        // 等待 hit 动画时长（按需调整）
        yield return new WaitForSeconds(0.3f);

        // 还原原材质
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].materials = originalMaterials[i];
        }
    }

    /// <summary>
    /// 冰冻状态 Shader：
    /// 追加冰冻材质，持续 duration 秒后还原
    /// </summary>
    private IEnumerator ApplyFrozenShader(GameObject target, float duration)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        var originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterials;

        // 追加冰冻 Shader
        for (int i = 0; i < renderers.Length; i++)
        {
            var mats = new Material[renderers[i].sharedMaterials.Length + 1];
            renderers[i].sharedMaterials.CopyTo(mats, 0);
            mats[mats.Length - 1] = config.iceFrozenShaderMaterial;
            renderers[i].materials = mats;
        }

        yield return new WaitForSeconds(duration);

        // 还原原材质
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].materials = originalMaterials[i];
        }
    }

    /// <summary>
    /// 死亡溶解 Shader：
    /// 将目标的材质换成溶解材质，从 0 → 1 驱动 _DissolveAmount，完成后隐藏对象
    /// </summary>
    private IEnumerator ApplyDissolveShader(GameObject target, float duration)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();

        // 实例化溶解材质，避免修改共享资产
        var dissolveMats = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            var origMats = renderers[i].sharedMaterials;
            dissolveMats[i] = new Material[origMats.Length + 1];
            origMats.CopyTo(dissolveMats[i], 0);
            dissolveMats[i][dissolveMats[i].Length - 1] = new Material(config.enemyDeathDissolveShaderMaterial);
            renderers[i].materials = dissolveMats[i];
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // 驱动溶解进度
            foreach (var rendererMats in dissolveMats)
            {
                var dissolveMat = rendererMats[rendererMats.Length - 1];
                if (dissolveMat != null)
                    dissolveMat.SetFloat(dissolvePropertyID, progress);
            }

            yield return null;
        }

        // 溶解完成后隐藏对象（由外部负责最终销毁）
        if (target != null)
            target.SetActive(false);
    }

    #endregion
}
