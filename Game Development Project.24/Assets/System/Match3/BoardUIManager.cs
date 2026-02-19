using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardUIManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 4;
    public int height = 5;
    public int typeCount = 4;

    [Header("UI References")]
    public RectTransform boardRoot;
    public TileItemUI tilePrefab;
    public Sprite[] typeSprites;

    [Header("Manual Layout (no GridLayoutGroup)")]
    public Vector2 cellSize = new Vector2(100, 100);
    public Vector2 spacing = new Vector2(10, 10);
    public Vector2 origin = new Vector2(0, 0);

    [Header("Animation")]
    public float moveDuration = 0.18f;
    public float clearDelay = 0.12f;

    [Header("Drag Preview")]
    public float dragThreshold = 25f;
    public float previewDuration = 0.08f;

    public RectTransform BoardRootRect => boardRoot;

    public bool InputLocked { get; private set; } = false;
    public bool IsDragging { get; private set; }


public void SetDragging(bool dragging)
{
    IsDragging = dragging;
}



    private BoardModel model;
    private TileItemUI[,] tiles;

    private TileItemUI selected;
    private bool isResolving = false;

    void Start()
    {
        model = new BoardModel(width, height, typeCount);
        model.FillRandom(avoidStartMatches: true);
        BuildUIFromModel();
    }

    private void BuildUIFromModel()
    {
        for (int i = boardRoot.childCount - 1; i >= 0; i--)
            Destroy(boardRoot.GetChild(i).gameObject);

        tiles = new TileItemUI[width, height];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var tile = Instantiate(tilePrefab, boardRoot);

            // ✅ Drag-only：不传点击回调
            tile.Init(x, y, null);

            int type = model.GetTypeAt(x, y);
            tile.SetSprite(typeSprites[type]);

            tile.Rect.anchoredPosition = GetCellPos(x, y);
            tiles[x, y] = tile;
        }
    }


public void HighlightSoloDrag(TileItemUI tile)
{
    if (InputLocked || tile == null) return;

    // 先清掉旧的 selected
    if (selected != null && selected != tile)
        selected.SetSelected(false);

    selected = tile;
    selected.SetSelected(true);
}
public void ClearAllPreviewAndSelection()
{
    if (selected != null)
    {
        selected.SetSelected(false);
        selected = null;
    }
}

    public Vector2 GetCellPos(int x, int y)
    {
        float px = origin.x + x * (cellSize.x + spacing.x);
        float py = origin.y - y * (cellSize.y + spacing.y);
        return new Vector2(px, py);
    }

    public bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    // ===== Input lock =====
    private void LockInput()
    {
        InputLocked = true;

        // ✅ 锁盘时清掉选中与高亮：保证“选择也不可以”
        if (selected != null)
        {
            selected.SetSelected(false);
            selected = null;
        }
    }

    private void UnlockInput()
    {
        InputLocked = false;
    }

    // ===== Drag allow =====
    public bool CanStartDrag(TileItemUI tile)
    {
        if (InputLocked) return false;
        if (isResolving) return false;

        // 没有 selected：可以拖任意一个
        if (selected == null) return true;

        // 有 selected：只允许拖 selected
        return selected == tile;
    }

    public bool IsDraggingAllowedFor(TileItemUI tile) => CanStartDrag(tile);

    // 拖动开始：设置 selected 并高亮
    public void SelectForDrag(TileItemUI tile)
    {
        if (InputLocked) return;

        if (selected != null && selected != tile)
            selected.SetSelected(false);

        selected = tile;
        selected.SetSelected(true);
    }

    // ===== Drag direction -> preview target =====
    public Vector2Int GetDragPreviewTarget(Vector2Int from, Vector2 delta)
    {
        if (delta.magnitude < dragThreshold) return from;

        Vector2Int dir;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            dir = delta.x > 0 ? new Vector2Int(1, 0) : new Vector2Int(-1, 0);
        else
            // 你的格子 y 向下增大；local delta.y >0 通常是向上拖
            dir = delta.y > 0 ? new Vector2Int(0, -1) : new Vector2Int(0, 1);

        Vector2Int to = from + dir;
        return InBounds(to.x, to.y) ? to : from;
    }

    // ===== Preview swap (visual only) =====
public void ShowSwapPreview(Vector2Int from, Vector2Int to)
{
    if (InputLocked) return;

    var a = tiles[from.x, from.y];
    var b = tiles[to.x, to.y];
    if (a == null || b == null || a.Rect == null || b.Rect == null) return;

    // ✅ 预览高亮：两边都亮
    a.SetSelected(true);
    b.SetSelected(true);

    StartCoroutine(MoveTo(a.Rect, GetCellPos(to.x, to.y), previewDuration));
    StartCoroutine(MoveTo(b.Rect, GetCellPos(from.x, from.y), previewDuration));
}

public void CancelSwapPreview(Vector2Int from, Vector2Int to)
{
    var a = tiles[from.x, from.y];
    var b = tiles[to.x, to.y];
    if (a == null || b == null) return;

    // ✅ 取消预览：只保留被拖拽的那一个高亮（from 位置的 tile）
    // 注意：此时 tiles[from] 仍然是拖拽的 tile（我们预览不改数组）
    a.SetSelected(true);
    b.SetSelected(false);

    StartCoroutine(MoveTo(a.Rect, GetCellPos(from.x, from.y), previewDuration));
    StartCoroutine(MoveTo(b.Rect, GetCellPos(to.x, to.y), previewDuration));
}



    // 无效拖拽：取消选中并复位（其实本来也没乱跑）
    public void CancelDragSelectionAndSnapBack(Vector2Int at)
    {
        if (selected != null)
        {
            selected.SetSelected(false);
            selected = null;
        }

        var t = tiles[at.x, at.y];
        if (t != null && t.Rect != null)
            StartCoroutine(MoveTo(t.Rect, GetCellPos(at.x, at.y), previewDuration));
    }

    // ===== Commit swap (real) =====
    public void CommitSwapByDrag(Vector2Int from, Vector2Int to)
    {
        ClearAllPreviewAndSelection();

        if (InputLocked) return;
        if (isResolving) return;

            var a = tiles[from.x, from.y];
    var b = tiles[to.x, to.y];
    if (a != null) a.SetSelected(false);
    if (b != null) b.SetSelected(false);

        bool adjacent = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) == 1;
        if (!adjacent)
        {
            CancelSwapPreview(from, to);
            CancelDragSelectionAndSnapBack(from);
            return;
        }

        // ✅ 先取消高亮（交换后进入 resolve）
        if (selected != null)
        {
            selected.SetSelected(false);
            selected = null;
        }

        int ax = from.x; int ay = from.y;
        int bx = to.x;   int by = to.y;

        // 逻辑交换
        model.Swap(ax, ay, bx, by);

        // UI 交换：交换引用与坐标
        var aTile = tiles[ax, ay];
        var bTile = tiles[bx, by];
        if (aTile == null || bTile == null) return;

        tiles[ax, ay] = bTile;
        tiles[bx, by] = aTile;

        bTile.SetCoord(ax, ay);
        aTile.SetCoord(bx, by);

        // 正式交换动画
        StartCoroutine(MoveTo(aTile.Rect, GetCellPos(bx, by), moveDuration));
        StartCoroutine(MoveTo(bTile.Rect, GetCellPos(ax, ay), moveDuration));

        StartCoroutine(ResolveBoardCoroutine());
    }

    // ===== Safe MoveTo (avoid destroyed rect) =====
    private IEnumerator MoveTo(RectTransform rect, Vector2 target, float duration)
    {
        if (rect == null) yield break;

        Vector2 start = rect.anchoredPosition;
        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, duration);

        while (t < 1f)
        {
            if (rect == null) yield break;
            t += Time.deltaTime * inv;
            rect.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        if (rect == null) yield break;
        rect.anchoredPosition = target;
    }

    private IEnumerator ResolveBoardCoroutine()
    {
        // ✅ 全程锁盘：直到所有连锁结束才解锁
        isResolving = true;
        LockInput();

        // 等交换动画结束
        yield return new WaitForSeconds(moveDuration);

        while (true)
        {
            HashSet<Vector2Int> matches = model.FindMatches();
            if (matches.Count == 0) break;

            // 1) 消除：逻辑置空 + UI Destroy
            model.ClearMatches(matches);

            foreach (var p in matches)
            {
                var t = tiles[p.x, p.y];
                if (t != null) Destroy(t.gameObject);
                tiles[p.x, p.y] = null;
            }

            yield return new WaitForSeconds(clearDelay);

            // 2) 重力：带 moves
            var moves = model.ApplyGravityWithMoves();
            if (moves.Count > 0)
            {
                foreach (var m in moves)
                {
                    var moving = tiles[m.from.x, m.from.y];
                    tiles[m.to.x, m.to.y] = moving;
                    tiles[m.from.x, m.from.y] = null;

                    if (moving != null)
                    {
                        moving.SetCoord(m.to.x, m.to.y);
                        StartCoroutine(MoveTo(moving.Rect, GetCellPos(m.to.x, m.to.y), moveDuration));
                    }
                }
                yield return new WaitForSeconds(moveDuration);
            }

            // 3) 补齐：新块从上方掉落
            var spawns = model.FillEmptiesRandomWithSpawns();
            if (spawns.Count > 0)
            {
                foreach (var p in spawns)
                {
                    int type = model.GetTypeAt(p.x, p.y);

                    var newTile = Instantiate(tilePrefab, boardRoot);
                    newTile.Init(p.x, p.y, null); // drag-only
                    newTile.SetSprite(typeSprites[type]);

                    tiles[p.x, p.y] = newTile;

                    newTile.Rect.anchoredPosition = GetCellPos(p.x, -2);
                    StartCoroutine(MoveTo(newTile.Rect, GetCellPos(p.x, p.y), moveDuration));
                }
                yield return new WaitForSeconds(moveDuration);
            }
        }

        isResolving = false;
        UnlockInput();
    }
}
