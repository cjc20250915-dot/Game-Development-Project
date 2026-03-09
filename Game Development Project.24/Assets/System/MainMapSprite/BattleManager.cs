using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public Transform[] spawnPoints;

    void Start()
    {
        NodeData node = GameRunManager.Instance.currentNode;

        if (node == null)
        {
            Debug.LogWarning("没有节点数据");
            return;
        }

        SpawnEnemies(node);
    }

    void SpawnEnemies(NodeData node)
    {
        int spawnIndex = 0;

        foreach (var wave in node.enemyWaves)
        {
            for (int i = 0; i < wave.count; i++)
            {
                if (spawnIndex >= spawnPoints.Length)
                    spawnIndex = 0;

                Instantiate(
                    wave.enemyPrefab,
                    spawnPoints[spawnIndex].position,
                    Quaternion.identity
                );

                spawnIndex++;
            }
        }
    }
}