using System.Collections.Generic;
using UnityEngine;

public class BoardModel
{
    public readonly int Width;
    public readonly int Height;
    public readonly int TypeCount;

    // -1 表示空
    private int[,] types;

    public BoardModel(int width, int height, int typeCount)
    {
        Width = width;
        Height = height;
        TypeCount = Mathf.Max(1, typeCount);
        types = new int[Width, Height];
        ClearAll();
    }

    public void ClearAll()
    {
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
            types[x, y] = -1;
    }

    public bool InBounds(int x, int y)
        => x >= 0 && x < Width && y >= 0 && y < Height;

    public int GetTypeAt(int x, int y)
    {
        if (!InBounds(x, y)) return -1;
        return types[x, y];
    }

    public void SetTypeAt(int x, int y, int type)
    {
        if (!InBounds(x, y)) return;
        types[x, y] = type;
    }

    public void Swap(int ax, int ay, int bx, int by)
    {
        if (!InBounds(ax, ay) || !InBounds(bx, by)) return;
        int temp = types[ax, ay];
        types[ax, ay] = types[bx, by];
        types[bx, by] = temp;
    }

    /// <summary>
    /// 生成整个棋盘
    /// </summary>
    /// <param name="avoidStartMatches">是否避免开局直接三连（推荐开）</param>
    public void FillRandom(bool avoidStartMatches = true)
    {
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            if (!avoidStartMatches)
            {
                types[x, y] = Random.Range(0, TypeCount);
            }
            else
            {
                types[x, y] = GetRandomTypeAvoidingImmediateMatch(x, y);
            }
        }
    }

    private int GetRandomTypeAvoidingImmediateMatch(int x, int y)
    {
        // 简单做法：最多尝试 N 次，挑一个不会立刻形成 “横向/纵向 3连” 的类型
        const int maxTries = 20;

        for (int i = 0; i < maxTries; i++)
        {
            int t = Random.Range(0, TypeCount);
            if (!WouldMakeImmediateMatch(x, y, t))
                return t;
        }

        // 实在找不到就随便给一个（typeCount 很小时可能会发生）
        return Random.Range(0, TypeCount);
    }

    private bool WouldMakeImmediateMatch(int x, int y, int t)
    {
        // 检查横向：左边两个如果都等于 t，则形成 3 连
        if (x >= 2 &&
            types[x - 1, y] == t &&
            types[x - 2, y] == t)
            return true;

        // 检查纵向：上面两个如果都等于 t，则形成 3 连
        if (y >= 2 &&
            types[x, y - 1] == t &&
            types[x, y - 2] == t)
            return true;

        return false;
    }

    /// <summary>
    /// 仅用于调试：把棋盘输出成字符串
    /// </summary>
    public string DebugToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                sb.Append(types[x, y].ToString().PadLeft(2));
                sb.Append(" ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
    public HashSet<Vector2Int> FindMatches()
    {
        var result = new HashSet<Vector2Int>();

        // 横向扫描
        for (int y = 0; y < Height; y++)
        {
            int runType = types[0, y];
            int runStartX = 0;

            for (int x = 1; x <= Width; x++)
            {
                int t = (x < Width) ? types[x, y] : int.MinValue; // 哨兵：强制结算最后一段
                bool same = (x < Width && t == runType && t != -1);

                if (same) continue;

                int runLen = x - runStartX;
                if (runType != -1 && runLen >= 3)
                {
                    for (int k = runStartX; k < x; k++)
                        result.Add(new Vector2Int(k, y));
                }

                // 开始新段
                if (x < Width)
                {
                    runType = types[x, y];
                    runStartX = x;
                }
            }
        }

        // 纵向扫描
        for (int x = 0; x < Width; x++)
        {
            int runType = types[x, 0];
            int runStartY = 0;

            for (int y = 1; y <= Height; y++)
            {
                int t = (y < Height) ? types[x, y] : int.MinValue;
                bool same = (y < Height && t == runType && t != -1);

                if (same) continue;

                int runLen = y - runStartY;
                if (runType != -1 && runLen >= 3)
                {
                    for (int k = runStartY; k < y; k++)
                        result.Add(new Vector2Int(x, k));
                }

                if (y < Height)
                {
                    runType = types[x, y];
                    runStartY = y;
                }
            }
        }

        return result;
    }

    // ====== 新增：消除（把匹配位置设为 -1）======
    public void ClearMatches(HashSet<Vector2Int> matches)
    {
        foreach (var p in matches)
        {
            if (!InBounds(p.x, p.y)) continue;
            types[p.x, p.y] = -1;
        }
    }

    // ====== 新增：重力下落（每列向下压实）======
    public void ApplyGravity()
    {
        for (int x = 0; x < Width; x++)
        {
            int writeY = Height - 1;

            for (int y = Height - 1; y >= 0; y--)
            {
                int t = types[x, y];
                if (t == -1) continue;

                if (y != writeY)
                {
                    types[x, writeY] = t;
                    types[x, y] = -1;
                }
                writeY--;
            }
        }
    }

    // ====== 新增：补齐空位（把 -1 随机填满）======
    public void FillEmptiesRandom()
    {
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            if (types[x, y] == -1)
                types[x, y] = Random.Range(0, TypeCount);
        }
    }

    public List<(Vector2Int from, Vector2Int to)> ApplyGravityWithMoves()
{
    var moves = new List<(Vector2Int from, Vector2Int to)>();

    for (int x = 0; x < Width; x++)
    {
        int writeY = Height - 1;

        for (int y = Height - 1; y >= 0; y--)
        {
            int t = types[x, y];
            if (t == -1) continue;

            if (y != writeY)
            {
                types[x, writeY] = t;
                types[x, y] = -1;
                moves.Add((new Vector2Int(x, y), new Vector2Int(x, writeY)));
            }
            writeY--;
        }
    }
    return moves;
}

public List<Vector2Int> FillEmptiesRandomWithSpawns()
{
    var spawned = new List<Vector2Int>();
    for (int x = 0; x < Width; x++)
    for (int y = 0; y < Height; y++)
    {
        if (types[x, y] != -1) continue;
        types[x, y] = Random.Range(0, TypeCount);
        spawned.Add(new Vector2Int(x, y));
    }
    return spawned;
}

}
