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

    Vector3 worldPos = target.position + offset;
    Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

    if (screenPos.z <= 0f)
    {
        gameObject.SetActive(false);
        return;
    }

    gameObject.SetActive(true);
    transform.position = screenPos;
}

    public void BindTarget(EnemyUnit enemy, Transform followTarget = null)
    {
        if (enemy == null) return;

        target = followTarget != null ? followTarget : enemy.transform;
        hpUI.BindEnemy(enemy);
    }
}