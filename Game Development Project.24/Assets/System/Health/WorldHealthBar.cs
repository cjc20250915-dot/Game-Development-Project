using UnityEngine;

[RequireComponent(typeof(HealthBarUI))]
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);

    private Camera cam;
    private HealthBarUI hpUI;

    private void Awake()
    {
        cam = Camera.main;
        hpUI = GetComponent<HealthBarUI>();
    }

    private void LateUpdate()
    {
        if (target == null || cam == null) return;

        Vector3 worldPos = target.position + offset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        bool visible = screenPos.z > 0f;
        gameObject.SetActive(visible);

        if (visible)
        {
            transform.position = screenPos;
        }
    }

    public void BindTarget(EnemyUnit enemy, Transform followTarget = null)
    {
        if (enemy == null) return;

        target = followTarget != null ? followTarget : enemy.transform;
        hpUI.BindEnemy(enemy);
    }
}