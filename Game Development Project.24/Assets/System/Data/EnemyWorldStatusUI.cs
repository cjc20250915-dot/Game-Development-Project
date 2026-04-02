using UnityEngine;

public class EnemyWorldStatusUI : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Status Icons")]
    [SerializeField] private GameObject defendIcon;

    private EnemyUnit targetEnemy;
    private Transform targetTransform;
    private Camera cam;

    public void Bind(EnemyUnit enemy, Transform followTarget, Vector3 offset)
    {
        targetEnemy = enemy;
        targetTransform = followTarget;
        worldOffset = offset;

        cam = Camera.main;
        RefreshIconsImmediate();
    }

    private void LateUpdate()
    {
        if (targetEnemy == null || targetTransform == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (cam == null)
            cam = Camera.main;

        UpdatePosition();
        RefreshIconsImmediate();
    }

    private void UpdatePosition()
    {
        if (cam == null) return;

        Vector3 worldPos = targetTransform.position + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // 在摄像机后方时隐藏
        bool visible = screenPos.z > 0f;
        gameObject.SetActive(visible);

        if (!visible) return;

        transform.position = screenPos;
    }

    private void RefreshIconsImmediate()
    {
        if (defendIcon != null)
            defendIcon.SetActive(targetEnemy != null && targetEnemy.IsDefending);
    }
}