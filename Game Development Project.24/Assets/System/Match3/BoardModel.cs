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
}
