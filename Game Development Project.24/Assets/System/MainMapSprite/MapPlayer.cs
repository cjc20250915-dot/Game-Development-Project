using System.Collections;
using UnityEngine;

public class MapPlayer : MonoBehaviour
{
    //"当前节点"
    public MapNode currentNode;

    //移动速度
    public float moveSpeed = 5f;

    //最大点击距离
    public float maxMoveDistance = 3f;

    //跳跃高度
    public float jumpHeight = 1.5f;

    //额外离地偏移
    public float extraGroundOffset = 0.05f;

    //跳跃形变
    public float squashAmount = 0.7f;
    public float stretchAmount = 1.2f;

    //点击提示
    public GameObject validClickIndicatorPrefab;
    public GameObject invalidClickIndicatorPrefab;
    public float invalidIndicatorLifeTime = 0.5f;
    public float indicatorGroundOffset = 0.02f;

    //音效
    public AudioSource audioSource;
    public AudioClip jumpSound;
    public AudioClip landSound;

    private Vector3 originalScale;
    private bool isMoving = false;
    private Vector3 targetPosition;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    // 当前有效目标提示
    private GameObject activeValidIndicator;

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
            // transform.position = startPos;
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
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 clickedPos = hit.point;
            clickedPos.y += GetGroundOffset();

            float distance = Vector3.Distance(transform.position, clickedPos);

            // 超出最大距离：显示无效提示，不移动
            if (distance > maxMoveDistance)
            {
                ShowInvalidIndicator(hit.point);
                return;
            }

            // 在范围内：显示有效提示，并开始移动
            targetPosition = clickedPos;
            ShowValidIndicator(hit.point);

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

    void ShowValidIndicator(Vector3 worldPos)
    {
        if (validClickIndicatorPrefab == null) return;

        // 如果上一个有效提示还在，先删掉
        if (activeValidIndicator != null)
        {
            Destroy(activeValidIndicator);
        }

        Vector3 spawnPos = worldPos;
        spawnPos.y += indicatorGroundOffset;

        activeValidIndicator = Instantiate(validClickIndicatorPrefab, spawnPos, Quaternion.identity);
    }

    void ShowInvalidIndicator(Vector3 worldPos)
    {
        if (invalidClickIndicatorPrefab == null) return;

        Vector3 spawnPos = worldPos;
        spawnPos.y += indicatorGroundOffset;

        GameObject fx = Instantiate(invalidClickIndicatorPrefab, spawnPos, Quaternion.identity);
        Destroy(fx, invalidIndicatorLifeTime);
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        isMoving = true;

        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);

        // 给时长做一个限制，避免过慢或过快
        float duration = Mathf.Clamp(distance / moveSpeed, 0.2f, 0.45f);
        float time = 0f;

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
        transform.localScale = originalScale;

        if (audioSource && landSound)
            audioSource.PlayOneShot(landSound);

        // 到达后移除有效提示
        if (activeValidIndicator != null)
        {
            Destroy(activeValidIndicator);
            activeValidIndicator = null;
        }

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxMoveDistance);
    }
}