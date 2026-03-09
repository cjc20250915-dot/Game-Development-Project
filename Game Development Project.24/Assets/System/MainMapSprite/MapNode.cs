using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNode : MonoBehaviour
{
    [Header("可连接节点（当前节点触发后，会解锁这些节点）")]
    public List<MapNode> connectedNodes = new List<MapNode>();

    [Header("节点数据")]
    public NodeData nodeData;

    [Header("是否访问过 / 是否已触发过")]
    public bool visited = false;

    [Header("当前是否解锁")]
    public bool isUnlocked = false;

    [Header("是否进入自动加载")]
    public bool autoLoadScene = true;

    [Header("Gizmos")]
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.yellow;
    public Color visitedColor = Color.green;
    public Color lineColor = Color.white;
    public float sphereSize = 0.3f;

    public void TriggerNode()
    {
        // 没解锁，不能触发
        if (!isUnlocked)
        {
            Debug.Log($"{name} 还没有解锁，不能触发。");
            return;
        }

        // 已经触发过，不能再次触发
        if (visited)
        {
            Debug.Log($"{name} 已经触发过，不能再次触发。");
            return;
        }

        visited = true;
        isUnlocked = false;

        // 解锁后续节点
        UnlockConnectedNodes();

        if (nodeData != null && !string.IsNullOrEmpty(nodeData.sceneName))
        {
            if (GameRunManager.Instance != null)
            {
                GameRunManager.Instance.currentNode = nodeData;
            }

            Debug.Log("进入节点：" + nodeData.nodeName);

            if (autoLoadScene)
            {
                SceneManager.LoadScene(nodeData.sceneName);
            }
        }
    }

    private void UnlockConnectedNodes()
    {
        foreach (var node in connectedNodes)
        {
            if (node != null && !node.visited)
            {
                node.isUnlocked = true;
                Debug.Log("解锁节点：" + node.name);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (visited)
            Gizmos.color = visitedColor;
        else if (isUnlocked)
            Gizmos.color = unlockedColor;
        else
            Gizmos.color = lockedColor;

        Gizmos.DrawSphere(transform.position, sphereSize);

        Gizmos.color = lineColor;

        foreach (var node in connectedNodes)
        {
            if (node != null)
            {
                Gizmos.DrawLine(transform.position, node.transform.position);
            }
        }
    }
}