using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNode : MonoBehaviour
{
    [Header("仅用于场景中拖拽后继节点")]
    public List<MapNode> nextNodes = new List<MapNode>();

    [Header("节点数据")]
    public NodeData nodeData;

    [Header("运行时状态")]
    public bool visited = false;
    public bool isUnlocked = false;

    [Header("是否自动切场景")]
    public bool autoLoadScene = true;

    [Header("显示")]
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.yellow;
    public Color visitedColor = Color.green;
    public Color lineColor = Color.white;
    public float sphereSize = 0.3f;

    [Header("过渡控制器，可为空")]
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

        // 把当前节点和它的后继节点一起记录到管理器
        GameRunManager.Instance.EnterNode(this);

        Debug.Log("进入节点：" + nodeData.nodeName);

        if (!autoLoadScene) return;
        if (string.IsNullOrEmpty(nodeData.sceneName)) return;

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