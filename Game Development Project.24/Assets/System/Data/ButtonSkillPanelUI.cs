using UnityEngine;

public class BattleSkillPanelUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AllySlotBoard allySlotBoard;

    [Header("4 Fixed Buttons")]
    [SerializeField] private SkillButtonUI button1;
    [SerializeField] private SkillButtonUI button2;
    [SerializeField] private SkillButtonUI button3;
    [SerializeField] private SkillButtonUI button4;

    private void OnEnable()
    {
        if (allySlotBoard != null)
            allySlotBoard.OnSlotsChanged += RefreshSkillButtons;
    }

    private void OnDisable()
    {
        if (allySlotBoard != null)
            allySlotBoard.OnSlotsChanged -= RefreshSkillButtons;
    }

    private void Start()
    {
        RefreshSkillButtons();
    }

    public void RefreshSkillButtons()
    {
        if (allySlotBoard == null)
        {
            Debug.LogWarning("[BattleSkillPanelUI] allySlotBoard is null.");
            ClearAllButtons();
            return;
        }

        AllyUnit ally1 = allySlotBoard.SlotA;
        AllyUnit ally2 = allySlotBoard.SlotB;

        BindButtonToSkill(button1, ally1, 0);
        BindButtonToSkill(button2, ally1, 1);
        BindButtonToSkill(button3, ally2, 0);
        BindButtonToSkill(button4, ally2, 1);
    }

    private void BindButtonToSkill(SkillButtonUI buttonUI, AllyUnit owner, int skillIndex)
    {
        if (buttonUI == null) return;

        if (owner == null)
        {
            buttonUI.ClearSkill();
            return;
        }

        SkillData skill = GetSkillAt(owner, skillIndex);
        buttonUI.BindSkill(owner, skill);
    }

    private SkillData GetSkillAt(AllyUnit ally, int index)
    {
        if (ally == null) return null;
        if (ally.skills == null) return null;
        if (index < 0 || index >= ally.skills.Count) return null;

        return ally.skills[index];
    }

    private void ClearAllButtons()
    {
        if (button1 != null) button1.ClearSkill();
        if (button2 != null) button2.ClearSkill();
        if (button3 != null) button3.ClearSkill();
        if (button4 != null) button4.ClearSkill();
    }
}