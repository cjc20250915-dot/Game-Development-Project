using UnityEngine;

public class BoardUIManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 4;
    public int height = 5;
    public int typeCount = 4; // 4种物品

    [Header("UI References")]
    public RectTransform boardRoot;   // 你挂 GridLayoutGroup 的那个物体
    public TileItemUI tilePrefab;     // TileItem 预制体（带 Image+Button+TileItemUI）
    public Sprite[] typeSprites;      // size = 4，对应 4 种图案

    private BoardModel model;
    private TileItemUI[,] tiles;

    private TileItemUI selected; // 先预留：后面做交换会用

    void Start()
    {
        // 基本检查
        if (typeSprites == null || typeSprites.Length < typeCount)
            Debug.LogError("typeSprites 数量不够！请在 Inspector 里放满 4 张 sprite。");

        model = new BoardModel(width, height, typeCount);
        model.FillRandom(avoidStartMatches: true);

        BuildUIFromModel();
    }

    private void BuildUIFromModel()
    {
        // 清空旧子物体（方便你反复运行/重建）
        for (int i = boardRoot.childCount - 1; i >= 0; i--)
            Destroy(boardRoot.GetChild(i).gameObject);

        tiles = new TileItemUI[width, height];

        // 注意：GridLayoutGroup 会按“子物体顺序”自动排布
        // 我们用 y 从 0->height-1，x 从 0->width-1 的顺序创建
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var tile = Instantiate(tilePrefab, boardRoot);
            tile.Init(x, y, OnTileClicked);

            int type = model.GetTypeAt(x, y);
            tile.SetSprite(typeSprites[type]);

            tiles[x, y] = tile;
        }
    }

    private void OnTileClicked(TileItemUI tile)
    {
        // 目前只测试点击通不通
        Debug.Log($"Clicked: ({tile.X},{tile.Y}) type={model.GetTypeAt(tile.X, tile.Y)}");

        // 下一步我们会在这里写：选中→再点一个→交换→刷新UI→匹配检测
    }
}
