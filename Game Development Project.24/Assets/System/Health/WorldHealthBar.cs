using UnityEngine;

[RequireComponent(typeof(HealthBarUI))]
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);

    private Camera cam;
    private HealthBarUI hpUI;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        cam = Camera.main;
        hpUI = GetComponent<HealthBarUI>();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Vector3 worldPos = target.position + offset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        bool visible = screenPos.z > 0f;

        // 不要 SetActive(false)：禁用后就不会再 LateUpdate，血条无法自动恢复。
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (visible)
            transform.position = screenPos;
    }

    public void BindTarget(EnemyUnit enemy, Transform followTarget = null)
    {
        if (enemy == null) return;

        target = followTarget != null ? followTarget : enemy.transform;
        hpUI.BindEnemy(enemy);
    }
}