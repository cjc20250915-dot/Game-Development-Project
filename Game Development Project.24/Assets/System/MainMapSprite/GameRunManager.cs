using UnityEngine;

public class GameRunManager : MonoBehaviour
{
    public static GameRunManager Instance;

    [Header("当前节点")]
    public NodeData currentNode;

    [Header("玩家金币")]
    public int playerGold;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}