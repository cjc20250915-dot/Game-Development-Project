using System.Collections;
using UnityEngine;

public class MapPlayer : MonoBehaviour
{
    public MapNode currentNode;

    //[Header("移动速度")]
    public float moveSpeed = 5f;

    //[Header("最大点击距离")]
    public float maxMoveDistance = 3f;

    //[Header("跳跃高度")]
    public float jumpHeight = 1.5f;

    //[Header("额外离地偏移")]
    public float extraGroundOffset = 0.05f;

    //[Header("地面判定：法线与世界上方向点积下限，墙面约 0，水平地面约 1")]
    public float minGroundUpDot = 0.85f;

    //[Header("跳跃形变")]
    public float squashAmount = 0.7f;
    public float stretchAmount = 1.2f;

    //[Header("点击提示")]
    public GameObject validClickIndicatorPrefab;
    public GameObject invalidClickIndicatorPrefab;
    public float invalidIndicatorLifeTime = 0.5f;
    public float indicatorGroundOffset = 0.02f;

    //[Header("音效")]
    public AudioSource audioSource;
    public AudioClip jumpSound;
    public AudioClip landSound;

    private Vector3 originalScale;
    private bool isMoving = false;
    private Vector3 targetPosition;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private GameObject activeValidIndicator;

    private void Start()
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

        if (GameRunManager.Instance != null &&
            GameRunManager.Instance.TryConsumeMainMapReturnPose(out Vector3 returnPos, out Quaternion returnRot))
        {
            transform.SetPositionAndRotation(returnPos, returnRot);
        }
    }

    private void Update()
    {
        if (isMoving) return;
        HandleMouseClick();
    }

    private void HandleMouseClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (Vector3.Dot(hit.normal, Vector3.up) < minGroundUpDot)
            {
                ShowInvalidIndicator(hit.point);
                return;
            }

            Vector3 clickedPos = hit.point;
            clickedPos.y += GetGroundOffset();

            float distance = Vector3.Distance(transform.position, clickedPos);

            if (distance > maxMoveDistance)
            {
                ShowInvalidIndicator(hit.point);
                return;
            }

            targetPosition = clickedPos;
            ShowValidIndicator(hit.point);
            StartCoroutine(MoveToPosition(targetPosition));
        }
    }

    private float GetGroundOffset()
    {
        if (capsule != null)
            return capsule.height * 0.5f + extraGroundOffset;

        return 1f;
    }

    private void ShowValidIndicator(Vector3 worldPos)
    {
        if (validClickIndicatorPrefab == null) return;

        if (activeValidIndicator != null)
            Destroy(activeValidIndicator);

        Vector3 spawnPos = worldPos;
        spawnPos.y += indicatorGroundOffset;
        activeValidIndicator = Instantiate(validClickIndicatorPrefab, spawnPos, Quaternion.identity);
    }

    private void ShowInvalidIndicator(Vector3 worldPos)
    {
        if (invalidClickIndicatorPrefab == null) return;

        Vector3 spawnPos = worldPos;
        spawnPos.y += indicatorGroundOffset;

        GameObject fx = Instantiate(invalidClickIndicatorPrefab, spawnPos, Quaternion.identity);
        Destroy(fx, invalidIndicatorLifeTime);
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        isMoving = true;

        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);
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

        if (activeValidIndicator != null)
        {
            Destroy(activeValidIndicator);
            activeValidIndicator = null;
        }

        isMoving = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        MapNode node = other.GetComponent<MapNode>();
        if (node == null) return;

        node.RefreshState();

        if (!node.isUnlocked) return;
        if (node.visited) return;

        currentNode = node;
        node.TriggerNode();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxMoveDistance);
    }
}