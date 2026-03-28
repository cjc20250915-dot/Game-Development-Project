using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NodeData", menuName = "STS/NodeData")]
public class NodeData : ScriptableObject
{
    public enum NodeType
    {
        Battle,
        Elite,
        Boss,
        Shop,
        Event,
        Rest
    }

    //[Header("节点唯一名称/ID")]
    public string nodeName;

    //[Header("节点类型")]
    public NodeType nodeType;

    //[Header("进入场景")]
    public string sceneName;

    //[Header("敌人配置")]
    public List<EnemyWave> enemyWaves;

    //[Header("完成该节点后解锁的下一个节点ID")]
    public List<string> nextNodeIds = new List<string>();
}

[System.Serializable]
public class EnemyWave
{
    public GameObject enemyPrefab;
    public int count;
}