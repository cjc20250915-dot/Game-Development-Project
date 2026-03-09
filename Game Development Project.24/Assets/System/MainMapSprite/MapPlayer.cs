using System.Collections;
using UnityEngine;

public class MapPlayer : MonoBehaviour
{
    [Header("当前节点")]
    public MapNode currentNode;

    [Header("移动速度")]
    public float moveSpeed = 5f;

    [Header("跳跃高度")]
    public float jumpHeight = 1.5f;

    [Header("跳跃时间")]
    public float jumpDuration = 0.4f;

    private bool isMoving = false;
    private Vector3 targetPosition;



    void Update()
    {
        if (isMoving) return;

        HandleMouseClick();
    }

    void HandleMouseClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPosition = hit.point;

            StartCoroutine(MoveToPosition(targetPosition));
        }
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        isMoving = true;

        Vector3 start = transform.position;

        float distance = Vector3.Distance(start, target);
        float duration = distance / moveSpeed;

        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            Vector3 pos = Vector3.Lerp(start, target, t);

            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            pos.y += height;

            transform.position = pos;

            yield return null;
        }

        transform.position = target;

        isMoving = false;
    }

    void OnTriggerEnter(Collider other)
    {
        MapNode node = other.GetComponent<MapNode>();

        if (node == null) return;
        if (!node.isUnlocked) return;
        if (node.visited) return;

        currentNode = node;

        Debug.Log("到达节点：" + node.name);

        node.TriggerNode();
    }
}