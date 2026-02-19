using UnityEngine;
using UnityEngine.EventSystems;

public class TileDragHandler : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private TileItemUI tile;
    private BoardUIManager board;

    private Vector2 pointerDownLocal;
    private Vector2Int from;
    private Vector2Int previewTo;
    private bool hasPreview;
    private bool highlighted;
    private bool didPreview = false;



    private void Awake()
    {
        tile = GetComponent<TileItemUI>();

        // 更稳：父级找不到就全场景找
        board = GetComponentInParent<BoardUIManager>();
        if (board == null) board = Object.FindFirstObjectByType<BoardUIManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (board == null || tile == null) return;
        if (board.InputLocked) return;
        if (!board.CanStartDrag(tile)) return;

        from = new Vector2Int(tile.X, tile.Y);
        previewTo = from;
        hasPreview = false;
        highlighted = false;
board.SetDragging(true);
    didPreview = false;

    // ✅ 仅高亮自己（无预览时单独高亮）
    board.HighlightSoloDrag(tile);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            board.BoardRootRect, eventData.position, eventData.pressEventCamera, out pointerDownLocal
        );
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (board == null || tile == null) return;
        if (board.InputLocked) return;
        if (!board.IsDraggingAllowedFor(tile)) return;

        // ✅ 不再移动 tile 跟随鼠标，只计算方向做预览
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            board.BoardRootRect, eventData.position, eventData.pressEventCamera, out local
        );

        Vector2 delta = local - pointerDownLocal;
        if (!highlighted && delta.magnitude >= board.dragThreshold)
{
    board.SelectForDrag(tile);   // ✅ 真拖起来才高亮
    highlighted = true;
}


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
            didPreview = true;
board.ShowSwapPreview(from, previewTo);

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
            return;
        }

        if (!highlighted)
    return;


        if (hasPreview && previewTo != from)
        {
            // 先取消预览，避免和正式交换动画打架
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
