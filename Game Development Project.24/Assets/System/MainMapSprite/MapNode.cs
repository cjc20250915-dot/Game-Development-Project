using System.Collections.Generic;
using UnityEngine;

public class MapNode : MonoBehaviour
{
    [Header("可前往的节点")]
    public List<MapNode> connectedNodes = new List<MapNode>();

    [Header("是否访问过")]
    public bool visited = false;

    [Header("要跳转的场景（留空则不跳转）")]
    public string sceneToLoad;

    [Header("是否进入就触发")]
    public bool autoLoadScene = true;

    [Header("Gizmos")]
    public Color lineColor = Color.white;
    public float sphereSize = 0.3f;

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