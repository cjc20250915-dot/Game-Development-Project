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

    private PostTransitionPresentationConfig pendingMapPresentation;

    /// <summary>需先离开任意 MapNode 触发体再进的闸：未完成战斗退出(Abort)，或<strong>战后传送回大地图</strong>(ConsumeReturnPose)，二者共用；Resolve Spawn 若在重叠内仍保持。</summary>
    private bool gateMapNodeBattleUntilLeaveTrigger;

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

        UnlockIndoorRootsIfNeeded(nodes);

        foreach (MapNode node in nodes)
        {
            node.RefreshState();
        }

        if (nodes.Length > 0)
        {
            Debug.Log($"???? {SceneManager.GetActiveScene().name} ??????????????????????{nodes.Length}");
        }
    }

    /// <summary>
    /// 室外 01_MainMap 的节点链通过 nextNodes 解锁；下一关 Indoor 场景无法被拖进上一份地图的 nextNodes，
    /// 且 DontDestroy 的 GM 只吃第一个场景的 firstUnlockedNode，关卡内第二个 GM会被销毁，
    /// 因此首次进入 Indoor 时把「没有其他节点指向」的根节点 Id 记入解锁集合（后续仍靠室内 nextNodes 链推进）。
    /// </summary>
    private void UnlockIndoorRootsIfNeeded(MapNode[] nodes)
    {
        if (nodes.Length == 0)
            return;
        if (!string.Equals(SceneManager.GetActiveScene().name, "Indoor", System.StringComparison.Ordinal))
            return;

        HashSet<string> referenced = new HashSet<string>();
        foreach (MapNode mn in nodes)
        {
            foreach (MapNode nx in mn.nextNodes)
            {
                if (nx == null || nx.nodeData == null)
                    continue;
                string id = nx.nodeData.nodeName;
                if (!string.IsNullOrEmpty(id))
                    referenced.Add(id);
            }
        }

        foreach (MapNode mn in nodes)
        {
            if (mn.nodeData == null)
                continue;
            string id = mn.nodeData.nodeName;
            if (string.IsNullOrEmpty(id))
                continue;
            if (referenced.Contains(id))
                continue;
            unlockedNodeIds.Add(id);
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

        if (currentNode.presentationAfterTransitionToMap != null &&
            currentNode.presentationAfterTransitionToMap.enabled &&
            currentNode.presentationAfterTransitionToMap.presentationVideo != null)
        {
            pendingMapPresentation = new PostTransitionPresentationConfig
            {
                enabled = true,
                presentationVideo = currentNode.presentationAfterTransitionToMap.presentationVideo,
                fadeInDuration = currentNode.presentationAfterTransitionToMap.fadeInDuration,
                fadeOutDuration = currentNode.presentationAfterTransitionToMap.fadeOutDuration
            };
            Debug.Log($"[GameRunManager] ????????????????? {currentId}??clip={pendingMapPresentation.presentationVideo?.name}");
        }
        else
        {
            pendingMapPresentation = null;
        }

        pendingNextNodeIds.Clear();
        currentNode = null;
    }

    public bool TryConsumePendingMapPresentation(out PostTransitionPresentationConfig pres)
    {
        pres = pendingMapPresentation;
        pendingMapPresentation = null;
        return pres != null && pres.enabled;
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
        pendingMapPresentation = null;
        gateMapNodeBattleUntilLeaveTrigger = false;

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
        Debug.Log("??????????");
    }

    /// <summary>????????????????????????????????</summary>
    public void SetMainMapReturnPose(Vector3 worldPosition, Quaternion worldRotation)
    {
        pendingMainMapReturnPosition = worldPosition;
        pendingMainMapReturnRotation = worldRotation;
        hasPendingMainMapReturnPose = true;
    }

    /// <summary>????????????????????????????????????</summary>
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

        // 战后回到大地图会先瞬移到节点旁；若出生时仍与该节点 Trigger 重叠，会先拦一次 TriggerNode（与 Abort 同源 gate），再走 Resolve。
        gateMapNodeBattleUntilLeaveTrigger = true;

        return true;
    }

    /// <summary>在仍有一场未结算战斗时调用（例如暂停里「回主菜单」）：清空 currentNode，并在大地图上要求先离开节点触发区再进入才进战斗。</summary>
    public void AbortUnfinishedBattleIfNeeded()
    {
        if (currentNode == null)
            return;

        pendingNextNodeIds.Clear();
        currentNode = null;
        gateMapNodeBattleUntilLeaveTrigger = true;
    }

    public bool ShouldBlockMapNodeAutoEnter()
    {
        return gateMapNodeBattleUntilLeaveTrigger;
    }

    /// <summary>玩家离开任意 MapNode 的触发碰撞体时调用，解除「需离开后再进」的限制。</summary>
    public void ClearMapNodeEnterGateAfterExitTrigger()
    {
        if (!gateMapNodeBattleUntilLeaveTrigger) return;
        gateMapNodeBattleUntilLeaveTrigger = false;
    }

    /// <summary>大地图生成玩家并摆好位置后调用：若出生点不在任何节点触发器内，则无需等待 OnTriggerExit。</summary>
    public void ResolveMapNodeGateOnMainMapSpawn(Collider playerCollider)
    {
        if (!gateMapNodeBattleUntilLeaveTrigger)
            return;

        if (playerCollider == null)
        {
            gateMapNodeBattleUntilLeaveTrigger = false;
            return;
        }

        Bounds b = playerCollider.bounds;
        const float pad = 0.05f;
        Vector3 halfExtents = b.extents + Vector3.one * pad;
        Collider[] cols = Physics.OverlapBox(b.center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

        if (cols == null || cols.Length == 0)
        {
            gateMapNodeBattleUntilLeaveTrigger = false;
            return;
        }

        bool insideAnyMapNodeOverlap = false;
        foreach (Collider c in cols)
        {
            if (c == null || c == playerCollider) continue;
            if (c.GetComponentInParent<MapNode>() != null)
                insideAnyMapNodeOverlap = true;
        }

        if (insideAnyMapNodeOverlap)
        {
            gateMapNodeBattleUntilLeaveTrigger = true;
            return;
        }

        gateMapNodeBattleUntilLeaveTrigger = false;
    }
}