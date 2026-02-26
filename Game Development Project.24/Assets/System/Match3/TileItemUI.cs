using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class TileItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;

    [Header("Selection Highlight (Color Only)")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.6f, 1f);
    [SerializeField] private bool usePrefabColorAsNormal = true;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Hover (Preview) Effect")]
    [SerializeField] private Color hoverColor = new Color(0.85f, 0.9f, 1f, 1f); // 悬浮变色（可调）
    [SerializeField] private float hoverScale = 1.08f;                         // 悬浮放大倍数（可调）
    [SerializeField] private float hoverAnimTime = 0.08f;                      // 动画时间（可调）

    private Button button;
    public RectTransform Rect { get; private set; }

    public int X { get; private set; }
    public int Y { get; private set; }

    private Action<TileItemUI> onClick;

    private bool isHovered = false;
    private bool isSelected = false;

    private Vector3 baseScale;
    private Coroutine scaleCo;
    private BoardUIManager board;


    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
        if (icon == null) icon = GetComponent<Image>();

        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
        }


        baseScale = Rect.localScale;

        if (icon != null && usePrefabColorAsNormal)
            normalColor = icon.color;

            board = GetComponentInParent<BoardUIManager>();
if (board == null) board = UnityEngine.Object.FindFirstObjectByType<BoardUIManager>();


    }

    public void Init(int x, int y, Action<TileItemUI> clickCallback)
    {
        SetCoord(x, y);
        onClick = clickCallback;

        button.onClick.RemoveAllListeners();
        button.interactable = true;     // 不禁用，避免变灰
        if (onClick != null)
            button.onClick.AddListener(() => onClick?.Invoke(this));
    }

    public void SetCoord(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void SetSprite(Sprite sprite)
    {
        if (icon == null) return;
        icon.sprite = sprite;
        icon.enabled = (sprite != null);
    }

    // ====== Selection (dragging) ======
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshVisual();
    }

    // ====== Hover ======
public void OnPointerEnter(PointerEventData eventData)
{
    // ✅ 正在拖拽时，忽略悬浮效果
    if (board != null && board.IsDragging) return;

        if (board != null && board.InputLocked) return;

isHovered = true;
    RefreshVisual();
}

public void OnPointerExit(PointerEventData eventData)
{
    if (board != null && board.IsDragging) return;

        if (board != null && board.InputLocked) return;

isHovered = false;
    RefreshVisual();
}


        // Force-clear hover state (used when board input is locked)
    public void ForceClearHover()
    {
        if (!isHovered) return;
        isHovered = false;
        RefreshVisual();
    }

// ====== Visual State ======
    private void RefreshVisual()
{
    bool suppressHover = (board != null && board.IsDragging);

    if (icon != null)
    {
        if (isSelected)
            icon.color = selectedColor;
        else if (!suppressHover && isHovered)
            icon.color = hoverColor;
        else
            icon.color = normalColor;
    }

    float targetScale = (!isSelected && isHovered && !suppressHover) ? hoverScale : 1f;

    if (scaleCo != null) StopCoroutine(scaleCo);
    scaleCo = StartCoroutine(ScaleTo(targetScale, hoverAnimTime));
}


    private System.Collections.IEnumerator ScaleTo(float targetScale, float time)
    {
        if (Rect == null) yield break;

        Vector3 start = Rect.localScale;
        Vector3 target = baseScale * targetScale;
        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, time);

        while (t < 1f)
        {
            if (Rect == null) yield break;
            t += Time.deltaTime * inv;
            Rect.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }

        if (Rect != null)
            Rect.localScale = target;
    }
}
