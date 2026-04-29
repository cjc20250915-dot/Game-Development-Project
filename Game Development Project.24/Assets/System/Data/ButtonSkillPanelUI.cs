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

    [Header("5th Button — Fixed Skill (not from ally.skills list)")]
    [SerializeField] private SkillButtonUI button5;
    [Tooltip("拖到 Project 里的 SkillData 资产")]
    [SerializeField] private SkillData fixedSkillAsset;
    [Tooltip("为空则在 Refresh 时用 SlotA → SlotB 上第一个存活友军的 SkillCaster")]
    [SerializeField] private SkillCaster fixedSkillCasterOverride;

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

        BindFixedSkillButton();
    }

    private void BindFixedSkillButton()
    {
        if (button5 == null)
            return;

        SkillCaster caster = ResolveFixedSkillCaster();
        if (fixedSkillAsset == null || caster == null)
        {
            button5.ClearSkill();
            return;
        }

        button5.BindFixedSkill(caster, fixedSkillAsset);
    }

    private SkillCaster ResolveFixedSkillCaster()
    {
        if (fixedSkillCasterOverride != null)
            return fixedSkillCasterOverride;

        if (allySlotBoard == null)
            return null;

        if (allySlotBoard.SlotA != null && !allySlotBoard.SlotA.IsDead)
        {
            SkillCaster c = allySlotBoard.SlotA.GetComponentInChildren<SkillCaster>(true);
            if (c != null)
                return c;
        }

        if (allySlotBoard.SlotB != null && !allySlotBoard.SlotB.IsDead)
        {
            SkillCaster c = allySlotBoard.SlotB.GetComponentInChildren<SkillCaster>(true);
            if (c != null)
                return c;
        }

        return null;
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
        if (button5 != null) button5.ClearSkill();
    }
}