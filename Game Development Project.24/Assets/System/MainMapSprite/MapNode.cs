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

    [Header("碰撞体线框（Scene / 带 Gizmos 的 Game 视图）")]
    [Tooltip("开启后在编辑器中画出本物体上 Collider 的线框，便于对齐点击区域。")]
    public bool showColliderWire = false;
    public Color colliderWireColor = new Color(0f, 1f, 1f, 0.9f);

    //"过渡控制器，可为空")]
    public TransitionController transitionController;

    [Header("战斗结算后隐藏子物体")]
    [Tooltip("拖入节点下的子物体（可多个）；该节点在任意一次战斗结束并回到大地图后会被关闭显示；新周目 ResetRunProgress 后会重新显示。")]
    [SerializeField] private GameObject[] hideChildrenAfterBattleReturn;

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

        ApplyHideChildrenAfterBattleReturn();
    }

    /// <summary>该节点已通过战斗结算（胜负皆可）回到大地图时，隐藏配置的子物体。</summary>
    private void ApplyHideChildrenAfterBattleReturn()
    {
        if (hideChildrenAfterBattleReturn == null || hideChildrenAfterBattleReturn.Length == 0) return;

        bool hide = visited;
        for (int i = 0; i < hideChildrenAfterBattleReturn.Length; i++)
        {
            GameObject go = hideChildrenAfterBattleReturn[i];
            if (go == null) continue;
            go.SetActive(!hide);
        }
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

        if (showColliderWire)
        {
            Gizmos.color = colliderWireColor;
            foreach (Collider col in GetComponents<Collider>())
            {
                if (col == null || !col.enabled) continue;
                DrawColliderWire(col);
            }
        }
    }

    private static void DrawColliderWire(Collider c)
    {
        switch (c)
        {
            case BoxCollider box:
                {
                    Matrix4x4 old = Gizmos.matrix;
                    Gizmos.matrix = box.transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                    Gizmos.matrix = old;
                    break;
                }
            case SphereCollider sphere:
                {
                    Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
                    float r = sphere.radius * MaxComponent(sphere.transform.lossyScale);
                    Gizmos.DrawWireSphere(worldCenter, r);
                    break;
                }
            case CapsuleCollider cap:
                Gizmos.DrawWireCube(cap.bounds.center, cap.bounds.size);
                break;
            default:
                Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
                break;
        }
    }

    private static float MaxComponent(Vector3 v)
    {
        return Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}