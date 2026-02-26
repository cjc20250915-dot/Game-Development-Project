using UnityEngine;
using UnityEngine.EventSystems;

public class TileDragHandler : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
{
    private TileItemUI tile;
    private BoardUIManager board;

    private Vector2 pointerDownLocal;
    private Vector2Int from;
    private Vector2Int previewTo;

    private bool hasPreview;
    private bool highlighted; // 是否真正超过阈值开始拖拽（真拖拽）

    private void Awake()
    {
        tile = GetComponent<TileItemUI>();

        board = GetComponentInParent<BoardUIManager>();
        if (board == null) board = Object.FindFirstObjectByType<BoardUIManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (board == null || tile == null) return;
        if (board.InputLocked) return;
        if (board.DragLocked) return;
        if (!board.CanStartDrag(tile)) return;

        
        // Prefer the board's authoritative mapping (prevents rare desync after fast invalid swaps)
        if (!board.TryGetCoordOfTile(tile, out from))
            from = new Vector2Int(tile.X, tile.Y);
        else
            tile.SetCoord(from.x, from.y);
        previewTo = from;
        hasPreview = false;
        highlighted = false;


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            board.BoardRootRect, eventData.position, eventData.pressEventCamera, out pointerDownLocal
        );

        // ❌ 不要在这里 SetDragging(true)，否则“点击一下不拖”也会进入拖拽态
        // board.SetDragging(true);
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (board == null || tile == null) return;
        if (board.InputLocked) return;
        if (board.DragLocked) return;
        if (!board.IsDraggingAllowedFor(tile)) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            board.BoardRootRect, eventData.position, eventData.pressEventCamera, out local
        );

        Vector2 delta = local - pointerDownLocal;

        // ✅ 只有超过阈值，才算“真拖拽”，此时才进入 dragging 状态
        if (!highlighted && delta.magnitude >= board.dragThreshold)
        {
            board.SetDragging(true);
            board.SelectForDrag(tile);
            highlighted = true;
        }

        // 还没超过阈值：不要做预览
        if (!highlighted) return;

        Vector2Int to = board.GetDragPreviewTarget(from, delta);

        if (to == from)
        {
            if (hasPreview)
            {
                board.CancelSwapPreview(from, previewTo);
                hasPreview = false;
                previewTo = from;
            }
            return;
        }

        if (!hasPreview || to != previewTo)
        {
            if (hasPreview) board.CancelSwapPreview(from, previewTo);

            previewTo = to;
            hasPreview = true;
            board.ShowSwapPreview(from, previewTo);
        }
    }

    // ✅ 关键：处理“只是点击一下/轻微移动但没达到阈值”的情况
    public void OnPointerUp(PointerEventData eventData)
    {
        if (board == null || tile == null) return;

        // 没有进入真拖拽：就把按下产生的高亮/状态清理掉，避免卡住
        if (!highlighted)
        {
            board.ClearAllPreviewAndSelection();
            board.SetDragging(false);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (board == null || tile == null) return;

        if (board.InputLocked)
        {
            if (hasPreview) board.CancelSwapPreview(from, previewTo);
            board.CancelDragSelectionAndSnapBack(from);

            hasPreview = false;
            previewTo = from;

            board.SetDragging(false);
            return;
        }

        // 没有真拖拽（没过阈值）：当作点击结束，清理状态
        if (!highlighted)
        {
            board.ClearAllPreviewAndSelection();
            board.SetDragging(false);
            return;
        }

        if (hasPreview && previewTo != from)
        {
            board.CancelSwapPreview(from, previewTo);
            board.CommitSwapByDrag(from, previewTo);
        }
        else
        {
            board.CancelDragSelectionAndSnapBack(from);
            board.ClearAllPreviewAndSelection();
        }

        hasPreview = false;
        previewTo = from;
        board.SetDragging(false);
    }
}