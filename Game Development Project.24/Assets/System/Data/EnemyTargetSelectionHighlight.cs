using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 选敌高亮：利用材质上的描边属性（如 Cartoon/CartoonRender 的 _OutlineColor / _OutlineWidth）显示黄色边框，悬停为蓝色。
/// </summary>
[DisallowMultipleComponent]
public class EnemyTargetSelectionHighlight : MonoBehaviour
{
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    [SerializeField] private EnemyUnit enemyUnit;

    [Header("Outline (Cartoon/CartoonRender 等带 _OutlineColor 的材质)")]
    [SerializeField] private Color selectableOutlineColor = new Color(1f, 0.85f, 0.1f, 1f);
    [SerializeField] private Color hoverOutlineColor = new Color(0.25f, 0.55f, 1f, 1f);
    [Tooltip("选敌时的描边宽度（与角色材质默认宽度独立，便于统一）")]
    [SerializeField] private float selectableOutlineWidth = 0.035f;
    [Tooltip("悬停时描边宽度，略大可更明显")]
    [SerializeField] private float hoverOutlineWidth = 0.045f;

    private MaterialPropertyBlock mpb;

    private struct OutlineSlot
    {
        public Renderer Renderer;
        public int MaterialIndex;
        public Color CachedColor;
        public float CachedWidth;
    }

    private OutlineSlot[] slots;
    private bool cacheBuilt;

    private void Awake()
    {
        if (enemyUnit == null)
            enemyUnit = GetComponentInParent<EnemyUnit>();

        mpb = new MaterialPropertyBlock();
    }

    private void BuildCache()
    {
        if (cacheBuilt) return;
        cacheBuilt = true;

        Transform tintRoot = enemyUnit != null ? enemyUnit.transform : transform;
        Renderer[] renderers = tintRoot.GetComponentsInChildren<Renderer>(true);

        var list = new List<OutlineSlot>();

        for (int ri = 0; ri < renderers.Length; ri++)
        {
            Renderer r = renderers[ri];
            if (r == null) continue;

            Material[] mats = r.sharedMaterials;
            if (mats == null) continue;

            for (int mi = 0; mi < mats.Length; mi++)
            {
                Material mat = mats[mi];
                if (mat == null) continue;
                if (!mat.HasProperty(OutlineColorId)) continue;

                Color c = mat.GetColor(OutlineColorId);
                float w = mat.HasProperty(OutlineWidthId) ? mat.GetFloat(OutlineWidthId) : selectableOutlineWidth;

                list.Add(new OutlineSlot
                {
                    Renderer = r,
                    MaterialIndex = mi,
                    CachedColor = c,
                    CachedWidth = w
                });
            }
        }

        slots = list.ToArray();

        if (slots.Length == 0)
            Debug.LogWarning("[EnemyTargetSelectionHighlight] 未找到带 _OutlineColor 的材质，无法显示选敌描边。敌人需使用 Cartoon/CartoonRender（或 Shader 中含 _OutlineColor / _OutlineWidth）。", this);
    }

    public void ApplyVisuals(bool selectionMode, bool isSelectableFront, bool hovered)
    {
        BuildCache();
        if (slots == null || slots.Length == 0) return;

        bool show = selectionMode && isSelectableFront;
        if (!show)
        {
            ClearOutline();
            return;
        }

        Color col = hovered ? hoverOutlineColor : selectableOutlineColor;
        float w = hovered ? hoverOutlineWidth : selectableOutlineWidth;

        for (int i = 0; i < slots.Length; i++)
        {
            OutlineSlot s = slots[i];
            if (s.Renderer == null) continue;

            s.Renderer.GetPropertyBlock(mpb, s.MaterialIndex);
            mpb.SetColor(OutlineColorId, col);
            if (s.Renderer.sharedMaterials[s.MaterialIndex] != null &&
                s.Renderer.sharedMaterials[s.MaterialIndex].HasProperty(OutlineWidthId))
                mpb.SetFloat(OutlineWidthId, w);
            s.Renderer.SetPropertyBlock(mpb, s.MaterialIndex);
        }
    }

    /// <summary>
    /// 恢复默认描边（来自材质资源上的值）
    /// </summary>
    public void ClearTint()
    {
        ClearOutline();
    }

    private void ClearOutline()
    {
        if (slots == null || slots.Length == 0) return;

        for (int i = 0; i < slots.Length; i++)
        {
            OutlineSlot s = slots[i];
            if (s.Renderer == null) continue;

            Material mat = s.Renderer.sharedMaterials[s.MaterialIndex];
            if (mat != null && mat.HasProperty(OutlineColorId))
            {
                s.Renderer.GetPropertyBlock(mpb, s.MaterialIndex);
                mpb.SetColor(OutlineColorId, s.CachedColor);
                if (mat.HasProperty(OutlineWidthId))
                    mpb.SetFloat(OutlineWidthId, s.CachedWidth);
                s.Renderer.SetPropertyBlock(mpb, s.MaterialIndex);
            }
            else
            {
                s.Renderer.SetPropertyBlock(null, s.MaterialIndex);
            }
        }
    }

    private void OnDestroy()
    {
        ClearTint();
    }
}
