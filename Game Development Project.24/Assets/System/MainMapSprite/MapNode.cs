using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MapNode : MonoBehaviour
{
    [Header("�����ӽڵ㣨��ǰ�ڵ㴥���󣬻������Щ�ڵ㣩")]
    public List<MapNode> connectedNodes = new List<MapNode>();

    [Header("�ڵ�����")]
    public NodeData nodeData;

    [Header("�Ƿ���ʹ� / �Ƿ��Ѵ�����")]
    public bool visited = false;

    [Header("��ǰ�Ƿ����")]
    public bool isUnlocked = false;

    [Header("�Ƿ�����Զ�����")]
    public bool autoLoadScene = true;

    [Header("Gizmos")]
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.yellow;
    public Color visitedColor = Color.green;
    public Color lineColor = Color.white;
    public float sphereSize = 0.3f;

    public TransitionController transitionController;

    public void TriggerNode()
    {
        // û���������ܴ���
        if (!isUnlocked)
        {
            Debug.Log($"{name} ��û�н��������ܴ�����");
            return;
        }

        // �Ѿ��������������ٴδ���
        if (visited)
        {
            Debug.Log($"{name} �Ѿ��������������ٴδ�����");
            return;
        }

        visited = true;
        isUnlocked = false;

        // ���������ڵ�
        UnlockConnectedNodes();

        if (nodeData != null && !string.IsNullOrEmpty(nodeData.sceneName))
        {
            if (GameRunManager.Instance != null)
            {
                GameRunManager.Instance.currentNode = nodeData;
            }

            Debug.Log("����ڵ㣺" + nodeData.nodeName);

           if (autoLoadScene)
{
    if (transitionController != null)
    {
        transitionController.LoadSceneWithTransition(nodeData.sceneName);
    }
    else
    {
        Debug.LogWarning("TransitionController 未设置，使用默认跳转");
        SceneManager.LoadScene(nodeData.sceneName);
    }
}
        }
    }

    private void UnlockConnectedNodes()
    {
        foreach (var node in connectedNodes)
        {
            if (node != null && !node.visited)
            {
                node.isUnlocked = true;
                Debug.Log("�����ڵ㣺" + node.name);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (visited)
            Gizmos.color = visitedColor;
        else if (isUnlocked)
            Gizmos.color = unlockedColor;
        else
            Gizmos.color = lockedColor;

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