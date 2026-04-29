using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

[System.Serializable]
public class PostTransitionPresentationConfig
{
    [Tooltip("勾选并指定视频：回主图后在独立视频层播放（低于转场 Canvas 的 Sort Order）")]
    public bool enabled;

    [Tooltip("导入工程后的 VideoClip")]
    public VideoClip presentationVideo;

    [Tooltip("视频层从透明到不透明的时长（秒，使用 unscaled）")]
    public float fadeInDuration = 0.5f;

    [Tooltip("播放结束后视频层从不透明到透明的时长（秒，使用 unscaled）")]
    public float fadeOutDuration = 0.5f;
}

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

    [Header("节点唯一名称/ID")]
    public string nodeName;

    [Header("节点类型")]
    public NodeType nodeType;

    [Header("进入场景")]
    public string sceneName;

    [Header("回到主图后的过场视频")]
    public PostTransitionPresentationConfig presentationAfterTransitionToMap = new PostTransitionPresentationConfig();

    [Header("敌人配置")]
    public List<EnemyWave> enemyWaves;
}

[System.Serializable]
public class EnemyWave
{
    public GameObject enemyPrefab;
    public int count;
}
