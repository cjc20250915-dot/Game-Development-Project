using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [Header("Data")]
    public SkillData skill;

    [Header("Auto Wired From Ally")]
    public AllyUnit allyUnit;
    public SkillCaster caster;

    [Header("UI")]
    public TMP_Text nameText;
    public Button button;

    private void Awake()
    {
        // Auto-find UI refs
        if (button == null) button = GetComponent<Button>();
        if (nameText == null) nameText = GetComponentInChildren<TMP_Text>(true);

        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }

        // Auto-find ally context if not bound yet
        AutoBindFromParentsIfNeeded();
        RefreshUI();
        RefreshInteractable();
    }

    private void OnEnable()
    {
        // 重新启用时确保绑定存在
        AutoBindFromParentsIfNeeded();
        HookAllyEvents(true);
        RefreshUI();
        RefreshInteractable();
    }

    private void OnDisable()
    {
        HookAllyEvents(false);
    }

    /// <summary>
    /// 由面板生成按钮后调用：把“这个按钮属于哪个角色、对应哪个技能”绑定上
    /// </summary>
    public void Bind(AllyUnit ally, SkillData skillData)
    {
        HookAllyEvents(false);

        allyUnit = ally;
        skill = skillData;
        caster = (allyUnit != null) ? allyUnit.GetComponent<SkillCaster>() : null;

        HookAllyEvents(true);

        RefreshUI();
        RefreshInteractable();
    }

    public void RefreshUI()
    {
        if (nameText != null && skill != null)
            nameText.text = skill.skillName;
    }

    public void RefreshInteractable()
    {
        if (button == null)
            return;

        // 角色不存在/已死亡/没有技能/没有施法器 → 禁用
        if (allyUnit == null || allyUnit.IsDead || skill == null || caster == null)
        {
            button.interactable = false;
            return;
        }

        // 如果你之后在 SkillCaster 里实现 CanCast(skill)（元素是否足够）
        // 这里可以一键接入：button.interactable = caster.CanCast(skill);
        button.interactable = true;
    }

    private void OnClick()
    {
        if (button == null || !button.interactable) return;
        if (caster == null || skill == null) return;

        caster.TryCast(skill);

        // 技能释放后可能元素不够了，刷新一次
        RefreshInteractable();
    }

    private void AutoBindFromParentsIfNeeded()
    {
        if (allyUnit == null)
            allyUnit = GetComponentInParent<AllyUnit>();

        if (caster == null && allyUnit != null)
            caster = allyUnit.GetComponent<SkillCaster>();
    }

    private void HookAllyEvents(bool hook)
    {
        if (allyUnit == null) return;

        if (hook)
        {
            allyUnit.OnDead += HandleAllyDead;
            allyUnit.OnHPChanged += HandleHPChanged;
        }
        else
        {
            allyUnit.OnDead -= HandleAllyDead;
            allyUnit.OnHPChanged -= HandleHPChanged;
        }
    }

    private void HandleAllyDead(AllyUnit dead)
    {
        RefreshInteractable();
    }

    private void HandleHPChanged(int cur, int max)
    {
        // 目前只是同步“是否死亡”相关的交互
        RefreshInteractable();
    }
}