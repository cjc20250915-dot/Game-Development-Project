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

    //[Header("朝向：仅在 XZ 水平面上面朝点击方向（绕 Y 轴，无俯仰）")]
    public bool faceClickDirection2D = true;

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

        if (!TryRaycastGroundClick(ray, out RaycastHit hit))
            return;

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

    /// <summary>沿射线由近到远查找第一个「作为地面」的命中：忽略本角色与带 MapNode 的物体，从而可点到节点下方的地面。</summary>
    private bool TryRaycastGroundClick(Ray ray, out RaycastHit groundHit)
    {
        groundHit = default;
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (h.collider == null) continue;
            if (h.collider.GetComponentInParent<MapPlayer>() == this) continue;
            if (h.collider.GetComponentInParent<MapNode>() != null) continue;

            groundHit = h;
            return true;
        }

        return false;
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
        if (faceClickDirection2D)
            FaceHorizontalToward(start, target);

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

    /// <summary>在水平面（忽略 Y）上朝目标方向旋转，仅绕 Y，等价于 2D 顶视角朝向。</summary>
    private void FaceHorizontalToward(Vector3 fromWorld, Vector3 toWorld)
    {
        Vector3 d = toWorld - fromWorld;
        d.y = 0f;
        if (d.sqrMagnitude < 1e-8f) return;

        transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
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