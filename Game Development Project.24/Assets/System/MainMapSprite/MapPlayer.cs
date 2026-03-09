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

    [Header("额外离地偏移")]
    public float extraGroundOffset = 0.05f;

    [Header("跳跃形变")]
    public float squashAmount = 0.7f;
    public float stretchAmount = 1.2f;

    private Vector3 originalScale;

    [Header("音效")]
    public AudioSource audioSource;
    public AudioClip jumpSound;
    public AudioClip landSound;

    private bool isMoving = false;
    private Vector3 targetPosition;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        originalScale = transform.localScale;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (currentNode != null)
        {
            Vector3 startPos = currentNode.transform.position;
            startPos.y += GetGroundOffset();
            //transform.position = startPos;
        }
    }

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
            targetPosition.y += GetGroundOffset();

            StartCoroutine(MoveToPosition(targetPosition));
        }
    }

    float GetGroundOffset()
    {
        if (capsule != null)
        {
            return capsule.height * 0.5f + extraGroundOffset;
        }

        return 1f;
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        isMoving = true;

        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);

        float duration = distance / moveSpeed;
        float time = 0f;

        // 起跳音效
        if (audioSource && jumpSound)
            audioSource.PlayOneShot(jumpSound);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Vector3 pos = Vector3.Lerp(start, target, t);

            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            pos.y += height;

            transform.position = pos;

            // -------- Squash & Stretch --------

            float stretch = Mathf.Sin(t * Mathf.PI);

            float yScale = Mathf.Lerp(1f, stretchAmount, stretch);
            float xzScale = Mathf.Lerp(1f, squashAmount, stretch);

            transform.localScale = new Vector3(
                originalScale.x * xzScale,
                originalScale.y * yScale,
                originalScale.z * xzScale
            );

            yield return null;
        }

        transform.position = target;

        // 恢复原始形状
        transform.localScale = originalScale;

        // 落地音效
        if (audioSource && landSound)
            audioSource.PlayOneShot(landSound);

        isMoving = false;
    }

    void OnTriggerEnter(Collider other)
    {
        MapNode node = other.GetComponent<MapNode>();

        if (node == null) return;
        if (!node.isUnlocked) return;
        if (node.visited) return;

        currentNode = node;
        node.TriggerNode();
    }
}