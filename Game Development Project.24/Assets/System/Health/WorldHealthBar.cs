using UnityEngine;

public class WorldHealthBar : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2, 0);

    Camera cam;

    void Start()
    {
        cam = Camera.main;

        //在这里绑定 EnemyUnit
        if (target != null)
        {
            EnemyUnit enemy = target.GetComponentInChildren<EnemyUnit>();

            if (enemy != null)
            {
                GetComponent<HealthBarUI>().BindEnemy(enemy);
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 worldPos = target.position + offset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        transform.position = screenPos;
    }
}