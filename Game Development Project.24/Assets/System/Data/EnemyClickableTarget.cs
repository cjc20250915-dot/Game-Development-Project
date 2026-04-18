using UnityEngine;

public class EnemyClickableTarget : MonoBehaviour
{
    [SerializeField] private EnemyUnit enemyUnit;
    [SerializeField] private EnemyTargetSelectionHighlight highlight;

    private bool hover;

    private void Awake()
    {
        if (enemyUnit == null)
            enemyUnit = GetComponentInParent<EnemyUnit>();

        if (enemyUnit != null)
            highlight = enemyUnit.GetComponentInChildren<EnemyTargetSelectionHighlight>(true);

        if (highlight == null)
            highlight = GetComponent<EnemyTargetSelectionHighlight>();

        if (highlight == null && enemyUnit != null)
            highlight = enemyUnit.gameObject.AddComponent<EnemyTargetSelectionHighlight>();
    }

    private void OnDisable()
    {
        hover = false;
        highlight?.ApplyVisuals(false, false, false);
    }

    private void LateUpdate()
    {
        if (highlight == null) return;

        EnemyTargetSelectionManager m = EnemyTargetSelectionManager.Instance;
        bool mode = m != null && m.IsSelectingTarget();
        bool selectable = mode && m.IsEnemySelectablePublic(enemyUnit);

        if (!mode)
            hover = false;

        highlight.ApplyVisuals(mode, selectable, selectable && hover);
    }

    private void OnMouseDown()
    {
        if (enemyUnit == null) return;
        if (enemyUnit.IsDead) return;

        if (EnemyTargetSelectionManager.Instance == null) return;

        EnemyTargetSelectionManager.Instance.TrySelectEnemy(enemyUnit);
    }

    private void OnMouseEnter()
    {
        if (enemyUnit == null || enemyUnit.IsDead) return;

        EnemyTargetSelectionManager m = EnemyTargetSelectionManager.Instance;
        if (m == null || !m.IsSelectingTarget()) return;
        if (!m.IsEnemySelectablePublic(enemyUnit)) return;

        hover = true;
    }

    private void OnMouseExit()
    {
        hover = false;
    }
}
