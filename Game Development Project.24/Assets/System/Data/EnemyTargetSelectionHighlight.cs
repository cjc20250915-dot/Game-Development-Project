using UnityEngine;

/// <summary>
/// 与 <see cref="EnemyClickableTarget"/> 配合：在选择模式下显示「可选中」与「悬停」两套提示（优先使用独立物体，否则用颜色叠加）。
/// </summary>
[DisallowMultipleComponent]
public class EnemyTargetSelectionHighlight : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private EnemyUnit enemyUnit;
    [Header("Optional VFX roots (assign in prefab)")]
    [SerializeField] private GameObject selectableHint;
    [SerializeField] private GameObject hoverHint;

    [Header("Tint fallback (when both hints are null)")]
    [SerializeField] private Color selectableTint = new Color(1f, 0.92f, 0.45f, 1f);
    [SerializeField] private Color hoverTint = new Color(1f, 1f, 0.75f, 1f);

    private Renderer[] renderers;
    private Color[] cachedBaseColors;
    private bool[] colorPropValid;
    private bool[] useBaseColorId;
    private MaterialPropertyBlock mpb;
    private bool tintCached;

    private void Awake()
    {
        if (enemyUnit == null)
            enemyUnit = GetComponentInParent<EnemyUnit>();

        mpb = new MaterialPropertyBlock();
    }

    private void EnsureTintCache()
    {
        if (tintCached) return;
        tintCached = true;

        Transform tintRoot = enemyUnit != null ? enemyUnit.transform : transform;
        renderers = tintRoot.GetComponentsInChildren<Renderer>(true);
        cachedBaseColors = new Color[renderers.Length];
        colorPropValid = new bool[renderers.Length];
        useBaseColorId = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            Material m = r.sharedMaterial;
            if (m == null) continue;

            if (m.HasProperty(BaseColorId))
            {
                cachedBaseColors[i] = m.GetColor(BaseColorId);
                colorPropValid[i] = true;
                useBaseColorId[i] = true;
            }
            else if (m.HasProperty(ColorId))
            {
                cachedBaseColors[i] = m.GetColor(ColorId);
                colorPropValid[i] = true;
                useBaseColorId[i] = false;
            }
        }
    }

    public void ApplyVisuals(bool selectionMode, bool isSelectableFront, bool hovered)
    {
        bool useHints = selectableHint != null || hoverHint != null;

        if (useHints)
        {
            ClearTint();
            bool showSelectable = selectionMode && isSelectableFront;
            bool showHover = showSelectable && hovered;
            bool hasHoverLayer = hoverHint != null;

            if (selectableHint != null)
                selectableHint.SetActive(showSelectable && (!hasHoverLayer || !showHover));

            if (hoverHint != null)
                hoverHint.SetActive(showHover);

            return;
        }

        EnsureTintCache();
        if (renderers == null || renderers.Length == 0) return;

        bool showTint = selectionMode && isSelectableFront;
        if (!showTint)
        {
            ClearTint();
            return;
        }

        Color mul = hovered ? hoverTint : selectableTint;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            if (!colorPropValid[i]) continue;

            r.GetPropertyBlock(mpb);
            Color c = cachedBaseColors[i] * mul;
            if (useBaseColorId[i])
                mpb.SetColor(BaseColorId, c);
            else
                mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(mpb);
        }
    }

    public void ClearTint()
    {
        if (selectableHint != null) selectableHint.SetActive(false);
        if (hoverHint != null) hoverHint.SetActive(false);

        if (renderers == null || renderers.Length == 0) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].SetPropertyBlock(null);
        }
    }
}
