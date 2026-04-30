using System.Collections.Generic;
using System.Collections;
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

    [Header("Locked Ring Visual")]
    [Tooltip("调试开关：当节点处于“未解锁且未完成”时在 Console 打印日志。")]
    [SerializeField] private bool logWhenLockedAndNotVisited = true;
    [Tooltip("要被替换的目标子物体名。")]
    [SerializeField] private string lockedReplaceChildName = "2d ring target 0 Green";
    [Tooltip("名称匹配方式：关闭=完全相等；开启=包含该字符串即可命中。")]
    [SerializeField] private bool lockedReplaceNameUseContains = false;
    [Tooltip("仅在“未解锁且未完成”时，用该 Prefab 替换目标子物体。")]
    [SerializeField] private GameObject lockedReplacePrefab;
    [SerializeField] private float delayedStateRefreshSeconds = 0.1f;

    private GameObject lockedReplaceOriginalObject;
    private GameObject lockedReplaceInstance;

    private void Awake()
    {
    }

    private void Start()
    {
        StartCoroutine(DelayedInitialRefresh());
    }

    private IEnumerator DelayedInitialRefresh()
    {
        if (delayedStateRefreshSeconds > 0f)
            yield return new WaitForSecondsRealtime(delayedStateRefreshSeconds);
        else
            yield return null; // 至少等一帧，规避初始化顺序问题

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
        RefreshBlockedRingMaterial();
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
            RefreshBlockedRingMaterial();
            Debug.Log($"{name} 未解锁，不能进入");
            return;
        }

        if (visited)
        {
            RefreshBlockedRingMaterial();
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

    private void RefreshBlockedRingMaterial()
    {
        // 仅在“未解锁且未完成（已完成不算）”时显示标记。
        bool shouldShowLockedMarker = !isUnlocked && !visited;
        if (shouldShowLockedMarker && logWhenLockedAndNotVisited)
        {
            string id = nodeData != null ? nodeData.nodeName : "(null)";
            Debug.Log($"[MapNode] Locked&NotVisited: {name}, nodeId={id}");
        }

        if (shouldShowLockedMarker)
            ShowLockedReplacement();
        else
            HideLockedReplacement();
    }

    private void ShowLockedReplacement()
    {
        CacheLockedReplaceOriginalObject();
        if (lockedReplaceOriginalObject == null || lockedReplacePrefab == null)
            return;

        lockedReplaceOriginalObject.SetActive(false);

        if (lockedReplaceInstance == null)
        {
            Transform original = lockedReplaceOriginalObject.transform;
            Transform parent = original.parent;
            lockedReplaceInstance = Instantiate(lockedReplacePrefab, parent);

            Transform t = lockedReplaceInstance.transform;
            t.localPosition = original.localPosition;
            t.localRotation = original.localRotation;
            t.localScale = original.localScale;
        }
        else
        {
            lockedReplaceInstance.SetActive(true);
        }
    }

    private void HideLockedReplacement()
    {
        if (lockedReplaceOriginalObject != null)
            lockedReplaceOriginalObject.SetActive(true);

        if (lockedReplaceInstance != null)
            lockedReplaceInstance.SetActive(false);
    }

    private void CacheLockedReplaceOriginalObject()
    {
        if (lockedReplaceOriginalObject != null)
            return;

        lockedReplaceOriginalObject = FindChildByConfiguredName()?.gameObject;
    }

    private Transform FindChildByConfiguredName()
    {
        if (string.IsNullOrWhiteSpace(lockedReplaceChildName))
            return null;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null || t == transform)
                continue;

            if (lockedReplaceNameUseContains)
            {
                if (t.name.Contains(lockedReplaceChildName))
                    return t;
            }
            else
            {
                if (t.name == lockedReplaceChildName)
                    return t;
            }
        }

        return null;
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