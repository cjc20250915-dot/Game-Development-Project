using System.Collections.Generic;
using UnityEngine;

public class BoardModel
{
    public readonly int Width;
    public readonly int Height;
    public readonly int TypeCount;

    // -1 表示空
    private int[,] types;
    // 生成权重（长度=TypeCount）
private float[] spawnWeights;


    public BoardModel(int width, int height, int typeCount)
    {
        Width = width;
        Height = height;
        TypeCount = Mathf.Max(1, typeCount);
        types = new int[Width, Height];
        spawnWeights = new float[TypeCount];
for (int i = 0; i < TypeCount; i++) spawnWeights[i] = 1f;

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
                types[x, y] = GetRandomTypeWeighted();

            }
            else
            {
                types[x, y] = GetRandomTypeAvoidingImmediateMatch(x, y);
            }
        }
    }

    public void SetSpawnWeights(IList<float> weights)
{
    if (weights == null || weights.Count == 0)
    {
        // fallback: uniform
        for (int i = 0; i < TypeCount; i++) spawnWeights[i] = 1f;
        return;
    }

    // 确保长度
    if (spawnWeights == null || spawnWeights.Length != TypeCount)
        spawnWeights = new float[TypeCount];

    float sum = 0f;
    for (int i = 0; i < TypeCount; i++)
    {
        float w = (i < weights.Count) ? Mathf.Max(0f, weights[i]) : 0f;
        spawnWeights[i] = w;
        sum += w;
    }

    // 全 0 的话 fallback
    if (sum <= 0.0001f)
    {
        for (int i = 0; i < TypeCount; i++) spawnWeights[i] = 1f;
    }
}

private int GetRandomTypeWeighted()
{
    // 确保可用
    if (spawnWeights == null || spawnWeights.Length != TypeCount)
    {
        spawnWeights = new float[TypeCount];
        for (int i = 0; i < TypeCount; i++) spawnWeights[i] = 1f;
    }

    float sum = 0f;
    for (int i = 0; i < TypeCount; i++) sum += Mathf.Max(0f, spawnWeights[i]);

    if (sum <= 0.0001f)
        return Random.Range(0, TypeCount);

    float r = Random.value * sum;
    float acc = 0f;

    for (int i = 0; i < TypeCount; i++)
    {
        acc += Mathf.Max(0f, spawnWeights[i]);
        if (r <= acc) return i;
    }

    return TypeCount - 1;
}

private int GetRandomTypeAvoidingImmediateMatch(int x, int y)
{
    const int maxTries = 20;

    for (int i = 0; i < maxTries; i++)
    {
        int t = GetRandomTypeWeighted();
        if (!WouldMakeImmediateMatch(x, y, t))
            return t; // ✅ 关键修复：返回这次抽到的安全类型
    }

    // 兜底：遍历所有类型，找一个不会立刻三连的
    for (int t = 0; t < TypeCount; t++)
    {
        if (!WouldMakeImmediateMatch(x, y, t))
            return t;
    }

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
                types[x, y] = GetRandomTypeWeighted();

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
