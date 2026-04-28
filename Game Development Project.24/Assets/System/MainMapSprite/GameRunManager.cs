using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRunManager : MonoBehaviour
{
    public static GameRunManager Instance;

    //[Header("??????????????????")]
    public NodeData currentNode;

    //[Header("??????????ID")]
    public MapNode firstUnlockedNode;

    //[Header("??????")]
    public int winCount = 0;
    public int loseCount = 0;

    private HashSet<string> completedNodeIds = new HashSet<string>();
    private HashSet<string> unlockedNodeIds = new HashSet<string>();

    // ?????????????????????ID
    private List<string> pendingNextNodeIds = new List<string>();

    // ??????????????????????????????????????????????????????? MapPlayer ????
    private bool hasPendingMainMapReturnPose;
    private Vector3 pendingMainMapReturnPosition;
    private Quaternion pendingMainMapReturnRotation;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (firstUnlockedNode != null && firstUnlockedNode.nodeData != null)
            {
                string firstId = firstUnlockedNode.nodeData.nodeName;

                if (!string.IsNullOrEmpty(firstId))
                {
                    unlockedNodeIds.Add(firstId);
                    Debug.Log("??????????" + firstId);
                }
                else
                {
                    Debug.LogWarning("???? nodeName ???");
                }
            }
            else
            {
                Debug.LogWarning("firstUnlockedNode ????????");
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
            Debug.Log($"???? {SceneManager.GetActiveScene().name} ??????????????????????{nodes.Length}");
        }
    }

    public void EnterNode(MapNode mapNode)
    {
        if (mapNode == null || mapNode.nodeData == null)
        {
            Debug.LogWarning("EnterNode ????mapNode ?? nodeData ???");
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

        Debug.Log("?????????????????????????" + currentNode.nodeName);
    }

    public void CompleteCurrentNodeAsWin()
    {
        winCount++;
        Debug.Log("????????????????????" + winCount);
        CompleteCurrentNode();
    }

    public void CompleteCurrentNodeAsLose()
    {
        loseCount++;
        Debug.Log("??????????????????" + loseCount);
        CompleteCurrentNode();
    }

    private void CompleteCurrentNode()
    {
        if (currentNode == null)
        {
            Debug.LogWarning("CompleteCurrentNode ????currentNode ???");
            return;
        }

        string currentId = currentNode.nodeName;

        if (string.IsNullOrEmpty(currentId))
        {
            Debug.LogWarning("CompleteCurrentNode ?????????? nodeName ???");
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
                Debug.Log("???????" + nextId);
            }
        }

        Debug.Log("?????" + currentId);

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
        hasPendingMainMapReturnPose = false;

        winCount = 0;
        loseCount = 0;

        if (firstUnlockedNode != null && firstUnlockedNode.nodeData != null)
        {
            string firstId = firstUnlockedNode.nodeData.nodeName;

            if (!string.IsNullOrEmpty(firstId))
            {
                unlockedNodeIds.Add(firstId);
            }
        }

        RefreshAllMapNodesInScene();
        Debug.Log("进度已重置");
    }

    /// <summary>在即将离开主图场景时调用（例如进入战斗）</summary>
    public void SetMainMapReturnPose(Vector3 worldPosition, Quaternion worldRotation)
    {
        pendingMainMapReturnPosition = worldPosition;
        pendingMainMapReturnRotation = worldRotation;
        hasPendingMainMapReturnPose = true;
    }

    /// <summary>回到主图后若存在记录则恢复玩家站位，并清除标记</summary>
    public bool TryConsumeMainMapReturnPose(out Vector3 worldPosition, out Quaternion worldRotation)
    {
        if (!hasPendingMainMapReturnPose)
        {
            worldPosition = default;
            worldRotation = default;
            return false;
        }

        worldPosition = pendingMainMapReturnPosition;
        worldRotation = pendingMainMapReturnRotation;
        hasPendingMainMapReturnPose = false;
        return true;
    }
}