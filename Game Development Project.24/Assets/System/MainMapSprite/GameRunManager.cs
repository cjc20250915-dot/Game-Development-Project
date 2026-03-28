using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRunManager : MonoBehaviour
{
    public static GameRunManager Instance;

    [Header("当前正在挑战的节点数据")]
    public NodeData currentNode;

    [Header("初始解锁节点ID")]
    public string firstUnlockedNodeId = "Node1";

    [Header("战斗统计")]
    public int winCount = 0;
    public int loseCount = 0;

    private HashSet<string> completedNodeIds = new HashSet<string>();
    private HashSet<string> unlockedNodeIds = new HashSet<string>();

    // 当前节点完成后要解锁的后继节点ID
    private List<string> pendingNextNodeIds = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!string.IsNullOrEmpty(firstUnlockedNodeId))
            {
                unlockedNodeIds.Add(firstUnlockedNodeId);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshAllMapNodesInScene();
    }

    public void RefreshAllMapNodesInScene()
    {
        MapNode[] nodes = FindObjectsByType<MapNode>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (MapNode node in nodes)
        {
            node.RefreshState();
        }

        if (nodes.Length > 0)
        {
            Debug.Log($"场景 {SceneManager.GetActiveScene().name} 中地图节点状态已刷新，数量：{nodes.Length}");
        }
    }

    public void EnterNode(MapNode mapNode)
    {
        if (mapNode == null || mapNode.nodeData == null)
        {
            Debug.LogWarning("EnterNode 失败：mapNode 或 nodeData 为空");
            return;
        }

        currentNode = mapNode.nodeData;
        pendingNextNodeIds.Clear();

        foreach (MapNode nextNode in mapNode.nextNodes)
        {
            if (nextNode == null || nextNode.nodeData == null) continue;

            string nextId = nextNode.nodeData.nodeName;
            if (!string.IsNullOrEmpty(nextId))
            {
                pendingNextNodeIds.Add(nextId);
            }
        }

        Debug.Log("进入节点（仅记录，不推进进度）：" + currentNode.nodeName);
    }

    public void CompleteCurrentNodeAsWin()
    {
        winCount++;
        Debug.Log("战斗胜利，当前胜利次数：" + winCount);
        CompleteCurrentNode();
    }

    public void CompleteCurrentNodeAsLose()
    {
        loseCount++;
        Debug.Log("战斗失败，当前失败次数：" + loseCount);
        CompleteCurrentNode();
    }

    private void CompleteCurrentNode()
    {
        if (currentNode == null)
        {
            Debug.LogWarning("CompleteCurrentNode 失败：currentNode 为空");
            return;
        }

        string currentId = currentNode.nodeName;

        if (string.IsNullOrEmpty(currentId))
        {
            Debug.LogWarning("CompleteCurrentNode 失败：当前节点 nodeName 为空");
            return;
        }

        completedNodeIds.Add(currentId);
        unlockedNodeIds.Remove(currentId);

        foreach (string nextId in pendingNextNodeIds)
        {
            if (string.IsNullOrEmpty(nextId)) continue;

            if (!completedNodeIds.Contains(nextId))
            {
                unlockedNodeIds.Add(nextId);
                Debug.Log("解锁节点：" + nextId);
            }
        }

        Debug.Log("完成节点：" + currentId);

        pendingNextNodeIds.Clear();
        currentNode = null;
    }

    public bool IsNodeCompleted(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        return completedNodeIds.Contains(nodeId);
    }

    public bool IsNodeUnlocked(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        return unlockedNodeIds.Contains(nodeId);
    }

    public void ResetRunProgress()
    {
        completedNodeIds.Clear();
        unlockedNodeIds.Clear();
        pendingNextNodeIds.Clear();
        currentNode = null;

        winCount = 0;
        loseCount = 0;

        if (!string.IsNullOrEmpty(firstUnlockedNodeId))
        {
            unlockedNodeIds.Add(firstUnlockedNodeId);
        }

        RefreshAllMapNodesInScene();
        Debug.Log("进度已重置");
    }
}