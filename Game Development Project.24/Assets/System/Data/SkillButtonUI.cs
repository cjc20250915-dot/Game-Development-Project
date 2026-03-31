using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private GameObject darkMask;

    private SkillData currentSkill;
    private AllyUnit owner;

    public SkillData CurrentSkill => currentSkill;
    public AllyUnit Owner => owner;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void BindSkill(AllyUnit newOwner, SkillData skill)
    {
        owner = newOwner;
        currentSkill = skill;

        bool hasSkill = owner != null && currentSkill != null;

        if (button != null)
            button.interactable = hasSkill;

        if (skillNameText != null)
            skillNameText.text = hasSkill ? currentSkill.skillName : "-";

        if (darkMask != null)
            darkMask.SetActive(!hasSkill);
    }

    public void ClearSkill()
    {
        BindSkill(null, null);
    }
}