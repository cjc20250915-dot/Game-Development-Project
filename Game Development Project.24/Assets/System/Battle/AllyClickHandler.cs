using UnityEngine;

public class AllyClickHandler : MonoBehaviour
{
    public AllyWorldSkillPanel worldPanel;

    private void Awake()
    {
        if (worldPanel == null) worldPanel = GetComponent<AllyWorldSkillPanel>();
    }

    private void OnMouseDown()
    {
        if (worldPanel == null) return;
        AllySelectionManager.Instance.Select(worldPanel);
    }
}