using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNode : MonoBehaviour
{
    [Header("可连接节点")]
    public List<MapNode> connectedNodes = new List<MapNode>();

    [Header("节点数据")]
    public NodeData nodeData;

    [Header("是否访问过")]
    public bool visited = false;

    [Header("是否进入自动加载")]
    public bool autoLoadScene = true;

    [Header("Gizmos")]
    public Color lineColor = Color.white;
    public float sphereSize = 0.3f;

    public void TriggerNode()
    {
        if (visited) return;

        visited = true;

        if (nodeData != null && !string.IsNullOrEmpty(nodeData.sceneName))
        {
            Debug.Log("进入节点：" + nodeData.nodeName);

            if (autoLoadScene)
            {
                SceneManager.LoadScene(nodeData.sceneName);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = visited ? Color.green : Color.yellow;
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