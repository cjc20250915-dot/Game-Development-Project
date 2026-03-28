using System.Collections.Generic;
using UnityEngine;

public class GameRunManager : MonoBehaviour
{
    public static GameRunManager Instance;

    //[Header("当前正在挑战的节点")]
    public NodeData currentNode;

    //[Header("初始解锁节点ID")]
    public string firstUnlockedNodeId = "Node1";

    private HashSet<string> completedNodeIds = new HashSet<string>();
    private HashSet<string> unlockedNodeIds = new HashSet<string>();

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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EnterNode(NodeData node)
    {
        if (node == null) return;

        currentNode = node;
        Debug.Log("进入节点（仅记录，不推进进度）：" + node.nodeName);
    }

    public void CompleteCurrentNode()
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

        if (currentNode.nextNodeIds != null)
        {
            foreach (string nextId in currentNode.nextNodeIds)
            {
                if (string.IsNullOrEmpty(nextId)) continue;

                if (!completedNodeIds.Contains(nextId))
                {
                    unlockedNodeIds.Add(nextId);
                    Debug.Log("解锁节点：" + nextId);
                }
            }
        }

        Debug.Log("完成节点：" + currentId);
    }

    public void QuitCurrentBattleWithoutProgress()
    {
        if (currentNode != null)
        {
            Debug.Log("中途退出战斗，不增加进度：" + currentNode.nodeName);
        }
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
        currentNode = null;

        if (!string.IsNullOrEmpty(firstUnlockedNodeId))
        {
            unlockedNodeIds.Add(firstUnlockedNodeId);
        }

        Debug.Log("进度已重置");
    }
}