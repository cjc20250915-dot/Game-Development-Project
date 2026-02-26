using UnityEngine;

public class AllySelectionManager : MonoBehaviour
{
    public static AllySelectionManager Instance;

    private AllyWorldSkillPanel current;

    private void Awake()
    {
        Instance = this;
    }

    public void Select(AllyWorldSkillPanel panel)
    {
        // 点同一个：可选做 Toggle
        if (current == panel)
        {
            current.Toggle();
            return;
        }

        // 关闭之前的
        if (current != null)
            current.Hide();

        current = panel;

        if (current != null)
            current.Show();
    }

    public void ClearSelection()
    {
        if (current != null)
            current.Hide();
        current = null;
    }
}