using UnityEngine;

public class EnemyClickableTarget : MonoBehaviour
{
    [SerializeField] private EnemyUnit enemyUnit;

    private void Awake()
    {
        if (enemyUnit == null)
            enemyUnit = GetComponentInParent<EnemyUnit>();
    }

    private void OnMouseDown()
    {
        if (enemyUnit == null) return;
        if (enemyUnit.IsDead) return;

        if (EnemyTargetSelectionManager.Instance == null) return;

        EnemyTargetSelectionManager.Instance.TrySelectEnemy(enemyUnit);
    }
}