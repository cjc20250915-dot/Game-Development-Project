using System.Collections.Generic;
using UnityEngine;

public class ScenePlayerSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        [Header("对应 NodeData 里的 nodeName")]
        public string nodeName;

        [Header("这个节点进入本场景后，玩家生成的位置")]
        public Transform spawnPoint;
    }

    [Header("玩家对象（可不填，留空则自动找 Tag=Player）")]
    public GameObject player;

    [Header("节点名 -> 出生点 对应表")]
    public List<SpawnEntry> spawnEntries = new List<SpawnEntry>();

    [Header("是否在 Start 时自动执行生成")]
    public bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        if (GameRunManager.Instance == null)
        {
            Debug.LogWarning("ScenePlayerSpawner: 没有找到 GameRunManager");
            return;
        }

        if (GameRunManager.Instance.currentNode == null)
        {
            Debug.LogWarning("ScenePlayerSpawner: currentNode 为空，无法确定生成点");
            return;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player == null)
        {
            Debug.LogWarning("ScenePlayerSpawner: 没有找到玩家对象，请确认玩家 Tag 是 Player，或手动拖引用");
            return;
        }

        string currentNodeName = GameRunManager.Instance.currentNode.nodeName;

        foreach (var entry in spawnEntries)
        {
            if (entry != null && entry.spawnPoint != null && entry.nodeName == currentNodeName)
            {
                player.transform.position = entry.spawnPoint.position;
                player.transform.rotation = entry.spawnPoint.rotation;

                Debug.Log($"玩家已生成到节点 {currentNodeName} 对应的位置：{entry.spawnPoint.name}");
                return;
            }
        }

        Debug.LogWarning($"ScenePlayerSpawner: 没有找到节点 {currentNodeName} 对应的生成点");
    }
}