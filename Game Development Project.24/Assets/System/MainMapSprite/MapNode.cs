using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNode : MonoBehaviour
{
    //"仅用于场景中拖拽后继节点")]
    public List<MapNode> nextNodes = new List<MapNode>();

    //"节点数据")]
    public NodeData nodeData;

    //"运行时状态")]
    public bool visited = false;
    public bool isUnlocked = false;

    //"是否自动切场景")]
    public bool autoLoadScene = true;

    //"显示")]
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.yellow;
    public Color visitedColor = Color.green;
    public Color lineColor = Color.white;
    public float sphereSize = 0.3f;

    //"过渡控制器，可为空")]
    public TransitionController transitionController;

    private void Start()
    {
        RefreshState();
    }

    public void RefreshState()
    {
        if (nodeData == null)
        {
            Debug.LogWarning($"{name} 没有配置 nodeData");
            return;
        }

        if (GameRunManager.Instance == null)
        {
            Debug.LogWarning("GameRunManager.Instance 不存在");
            return;
        }

        string nodeId = nodeData.nodeName;
        visited = GameRunManager.Instance.IsNodeCompleted(nodeId);
        isUnlocked = GameRunManager.Instance.IsNodeUnlocked(nodeId);
    }

    public void TriggerNode()
    {
        RefreshState();

        Debug.Log($"节点 {name} 尝试触发, nodeName={nodeData?.nodeName}, isUnlocked={isUnlocked}, visited={visited}");

        if (!isUnlocked)
        {
            Debug.Log($"{name} 未解锁，不能进入");
            return;
        }

        if (visited)
        {
            Debug.Log($"{name} 已完成，不能再次进入");
            return;
        }

        if (nodeData == null)
        {
            Debug.LogWarning($"{name} 没有 nodeData");
            return;
        }

        GameRunManager.Instance.EnterNode(this);

        Debug.Log("进入节点：" + nodeData.nodeName);
        Debug.Log("准备加载场景：" + nodeData.sceneName);

        if (!autoLoadScene) return;
        if (string.IsNullOrEmpty(nodeData.sceneName)) return;

        if (nodeData.nodeType == NodeData.NodeType.Battle)
        {
            MapPlayer mapPlayer = FindFirstObjectByType<MapPlayer>();
            if (mapPlayer != null)
                GameRunManager.Instance.SetMainMapReturnPose(mapPlayer.transform.position, mapPlayer.transform.rotation);
        }

        if (transitionController != null)
        {
            transitionController.LoadSceneWithTransition(nodeData.sceneName);
        }
        else
        {
            SceneManager.LoadScene(nodeData.sceneName);
        }
    }

    private void OnDrawGizmos()
    {
        if (visited)
            Gizmos.color = visitedColor;
        else if (isUnlocked)
            Gizmos.color = unlockedColor;
        else
            Gizmos.color = lockedColor;

        Gizmos.DrawSphere(transform.position, sphereSize);

        Gizmos.color = lineColor;
        foreach (var node in nextNodes)
        {
            if (node != null)
            {
                Gizmos.DrawLine(transform.position, node.transform.position);
            }
        }
    }
}