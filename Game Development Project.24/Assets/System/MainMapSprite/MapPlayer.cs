using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapPlayer : MonoBehaviour
{
    [Header("起始节点")]
    public MapNode currentNode;

    [Header("跳跃设置")]
    public float jumpHeight = 1.5f;
    public float jumpDuration = 0.4f;

    private bool isMoving = false;

    void Start()
    {
        if (currentNode != null)
        {
            transform.position = currentNode.transform.position;
            currentNode.visited = true;
        }
    }

    void Update()
    {
        if (isMoving) return;

        HandleInput();
    }

    void HandleInput()
    {
        if (currentNode == null) return;

        var connections = currentNode.connectedNodes;

        if (connections.Count == 0) return;

        // 按数字键选择分支
        for (int i = 0; i < connections.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                MoveToNode(connections[i]);
                break;
            }
        }
    }

    void MoveToNode(MapNode targetNode)
    {
        if (targetNode == null) return;

        StartCoroutine(JumpToNode(targetNode));
    }


    IEnumerator JumpToNode(MapNode target)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = target.transform.position;

        float time = 0;

        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float t = time / jumpDuration;

            Vector3 pos = Vector3.Lerp(start, end, t);

            // 抛物线高度
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            pos.y += height;

            transform.position = pos;

            yield return null;
        }

        transform.position = end;

        currentNode = target;
        currentNode.visited = true;

        Debug.Log("到达节点：" + target.name);

        TryLoadScene(currentNode);

        isMoving = false;
    }

    void TryLoadScene(MapNode node)
    {
        if (node.autoLoadScene && !string.IsNullOrEmpty(node.sceneToLoad))
        {
            Debug.Log("加载场景: " + node.sceneToLoad);
            SceneManager.LoadScene(node.sceneToLoad);
        }
    }
}
