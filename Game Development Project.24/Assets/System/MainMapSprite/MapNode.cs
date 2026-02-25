using System.Collections.Generic;
using UnityEngine;

public class MapNode : MonoBehaviour
{
    [Header("可前往的节点（手动拖拽）")]
    public List<MapNode> connectedNodes = new List<MapNode>();

    [Header("是否已经访问")]
    public bool visited = false;

    [Header("Gizmos 设置")]
    public Color lineColor = Color.white;
    public float sphereSize = 0.3f;

    void OnDrawGizmos()
    {
        // 画自己
        Gizmos.color = visited ? Color.green : Color.yellow;
        Gizmos.DrawSphere(transform.position, sphereSize);

        // 画连线
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
