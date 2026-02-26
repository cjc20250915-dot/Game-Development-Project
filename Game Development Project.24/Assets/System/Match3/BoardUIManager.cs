using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Input Cooldown")]
[SerializeField] private float postMoveInputCooldown = 0.3f;

// Compatibility: old code may still check DragLocked.
// Now we lock ALL input (hover/click/drag) via InputLocked + raycast disabling.
public bool DragLocked => InputLocked;

private bool pendingCooldownAfterResolve = false;

private ClearedElementTrackerUI_TMP clearedTracker;

public void QueueCooldownAfterResolve()
{
    pendingCooldownAfterResolve = true;
}

private IEnumerator PostResolveCooldownRoutine()
{
    // Keep everything locked for a short cooldown after the whole resolve finishes.
    LockInput();
    yield return new WaitForSeconds(postMoveInputCooldown);
    // If player's moves are depleted, switch to enemy turn instead of re-enabling input.
    if (turn != null && turn.IsPlayerTurn && turn.RemainingMoves <= 0)
    {
        // Keep board locked; enemy turn will run.
        turn.StartEnemyTurn();
        yield break;
    }
    UnlockInput();
}

// 在“消除/下落/补充/连锁”全部完成后调用
public void NotifyResolveFinished()
{
    if (pendingCooldownAfterResolve)
    {
        pendingCooldownAfterResolve = false;

        if (postCooldownCo != null) StopCoroutine(postCooldownCo);
        postCooldownCo = StartCoroutine(PostResolveCooldownRoutine());
    }
    else
    {
        // No cooldown queued: unlock immediately.
        // If player's moves are depleted, switch to enemy turn instead of unlocking.
        if (turn != null && turn.IsPlayerTurn && turn.RemainingMoves <= 0)
        {
            // Keep board locked; enemy turn will manage flow.
            turn.StartEnemyTurn();
        }
        else
        {
            UnlockInput();
        }
    }
}

    [Header("Spawn Weights (Type 0..3)")]
public float[] spawnWeights = new float[4] { 1f, 1f, 1f, 1f };


    public RectTransform BoardRootRect => boardRoot;

    // Disable all UI raycasts on the board while locked (prevents hover/click/drag during animations)
    private CanvasGroup boardCanvasGroup;
    private Coroutine postCooldownCo;

    public bool InputLocked { get; private set; } = false;
    public bool IsDragging { get; private set; }

    


public void SetDragging(bool dragging)
{
    IsDragging = dragging;
}



    private TurnBattleManager turn;
    private BoardModel model;
    private TileItemUI[,] tiles;

    // ===== Move coroutine guard (avoid multiple MoveTo fighting on same RectTransform) =====
    private readonly Dictionary<RectTransform, Coroutine> moveCos = new Dictionary<RectTransform, Coroutine>();
    private TileItemUI selected;
    private bool isResolving = false;

    // ===== Remember last committed swap (for swap-back when no match) =====
    private bool hasLastSwap = false;
    private Vector2Int lastSwapFrom;
    private Vector2Int lastSwapTo;


    

    void Start()
    {
        clearedTracker = FindFirstObjectByType<ClearedElementTrackerUI_TMP>();
        model = new BoardModel(width, height, typeCount);
        turn = FindFirstObjectByType<TurnBattleManager>();
        EnsureBoardCanvasGroup();
        // 确保长度正确（typeCount=4时就是4）
if (spawnWeights == null || spawnWeights.Length != typeCount)
{
    spawnWeights = new float[typeCount];
    for (int i = 0; i < typeCount; i++) spawnWeights[i] = 1f;
}

model.SetSpawnWeights(spawnWeights);
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


    // ===== Coordinate sanity: find a tile's real grid index (slow but called rarely) =====
    public bool TryGetCoordOfTile(TileItemUI target, out Vector2Int coord)
    {
        coord = default;
        if (target == null || tiles == null) return false;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (tiles[x, y] == target)
                {
                    coord = new Vector2Int(x, y);
                    return true;
                }
            }
        }
        return false;
    }


    // ===== Input lock =====
    private void EnsureBoardCanvasGroup()
    {
        if (boardRoot == null) return;
        if (boardCanvasGroup == null)
        {
            boardCanvasGroup = boardRoot.GetComponent<CanvasGroup>();
            if (boardCanvasGroup == null) boardCanvasGroup = boardRoot.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void LockInput()
    {
        InputLocked = true;
        EnsureBoardCanvasGroup();

        // Disable raycasts so mouse movement cannot trigger hover/click/drag during animations/resolves.
        if (boardCanvasGroup != null)
        {
            boardCanvasGroup.interactable = false;
            boardCanvasGroup.blocksRaycasts = false;
        }

        // Clear selection & hover visuals to prevent "stuck" highlight while locked.
        if (selected != null)
        {
            selected.SetSelected(false);
            selected = null;
        }

        if (tiles != null)
        {
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var t = tiles[x, y];
                if (t != null) t.ForceClearHover();
            }
        }
    }

    private void UnlockInput()
{
    InputLocked = false;
    EnsureBoardCanvasGroup();

    if (boardCanvasGroup != null)
    {
        boardCanvasGroup.interactable = true;
        boardCanvasGroup.blocksRaycasts = true;
    }
}

// ===== External control (turn system) =====
public void SetBoardInputEnabled(bool enabled)
{
    if (enabled) UnlockInput();
    else LockInput();
}

// ===== Drag allow =====
public bool CanStartDrag(TileItemUI tile)
{
    if (DragLocked) return false;
    if (InputLocked) return false;
    if (isResolving) return false;

    if (selected == null) return true;
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

    StartMove(a.Rect, GetCellPos(to.x, to.y), previewDuration);
    StartMove(b.Rect, GetCellPos(from.x, from.y), previewDuration);
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

    StartMove(a.Rect, GetCellPos(from.x, from.y), previewDuration);
    StartMove(b.Rect, GetCellPos(to.x, to.y), previewDuration);
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
            StartMove(t.Rect, GetCellPos(at.x, at.y), previewDuration);
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
        // ===== Turn system: consume 1 move per committed swap =====
        if (turn != null)
        {
            // Not player's turn or no moves left -> ignore this swap
            if (!turn.TryConsumePlayerMove())
            {
                CancelDragSelectionAndSnapBack(from);
                return;
            }
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
        // 记录本次交换（用于无消除时回退）
        hasLastSwap = true;
        lastSwapFrom = from;
        lastSwapTo = to;

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
        StartMove(aTile.Rect, GetCellPos(bx, by), moveDuration);
        StartMove(bTile.Rect, GetCellPos(ax, ay), moveDuration);

        QueueCooldownAfterResolve();   // ✅ 第三步：本次拖拽结束后需要 0.2s 冷却（等resolve完再开始）
        StartCoroutine(ResolveBoardCoroutine());
    }
    // ===== Move helper (cancel previous move on same RectTransform) =====
    private void StartMove(RectTransform rect, Vector2 target, float duration)
    {
        if (rect == null) return;

        if (moveCos.TryGetValue(rect, out var co) && co != null)
            StopCoroutine(co);

        moveCos[rect] = StartCoroutine(MoveTo(rect, target, duration));
    }

    // ===== Snap all tiles to their grid positions (safety) =====
    private void SnapAllTilesToGrid()
    {
        if (tiles == null) return;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var t = tiles[x, y];
            if (t == null || t.Rect == null) continue;
            t.SetCoord(x, y);
            t.Rect.anchoredPosition = GetCellPos(x, y);
        }
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

        // ===== 如果本次交换没有产生任何消除：回退交换 =====
        var firstMatches = model.FindMatches();
        if (firstMatches.Count == 0 && hasLastSwap)
        {
            Vector2Int f = lastSwapFrom;
            Vector2Int t = lastSwapTo;

            // 回退逻辑
            model.Swap(f.x, f.y, t.x, t.y);

            // 回退 UI 数组与坐标
            var aTile = tiles[f.x, f.y];
            var bTile = tiles[t.x, t.y];
            if (aTile != null && bTile != null)
            {
                tiles[f.x, f.y] = bTile;
                tiles[t.x, t.y] = aTile;

                bTile.SetCoord(f.x, f.y);
                aTile.SetCoord(t.x, t.y);

                // 回退动画
                if (aTile.Rect != null) StartMove(aTile.Rect, GetCellPos(t.x, t.y), moveDuration);
                if (bTile.Rect != null) StartMove(bTile.Rect, GetCellPos(f.x, f.y), moveDuration);
                yield return new WaitForSeconds(moveDuration);
            }

            SnapAllTilesToGrid();

            hasLastSwap = false;
            isResolving = false;
            NotifyResolveFinished();
            yield break;
        }

        hasLastSwap = false;


        while (true)
        {
            HashSet<Vector2Int> matches = model.FindMatches();
            if (matches.Count == 0) break;

            // 1) 消除：先统计类型，再逻辑置空 + UI Destroy
            if (clearedTracker != null)
            {
                foreach (var p in matches)
                {
                    // 注意：必须在 ClearMatches 之前读取类型（ClearMatches 会把 type 置为 -1）
                    int type = model.GetTypeAt(p.x, p.y);
                    clearedTracker.Add(type, 1);
                }
            }

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
                        StartMove(moving.Rect, GetCellPos(m.to.x, m.to.y), moveDuration);
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
                    StartMove(newTile.Rect, GetCellPos(p.x, p.y), moveDuration);
                }
                yield return new WaitForSeconds(moveDuration);
            }
        }

        SnapAllTilesToGrid();

        isResolving = false;
        NotifyResolveFinished();   // ✅ 第四步：resolve全部结束 -> 如果之前排队了冷却，这里开始0.2s冷却
    }
}