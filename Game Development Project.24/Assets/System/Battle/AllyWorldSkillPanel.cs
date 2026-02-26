using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AllyWorldSkillPanel : MonoBehaviour
{
    [System.Serializable]
    public class PanelSlot
    {
        public GameObject panelRoot;      // 面片根物体（默认隐藏）
        public TMP_Text titleText;        // 标题（可选）
        public Transform buttonContainer; // 放一个按钮就够
    }

    [Header("References")]
    public AllyUnit allyUnit;
    public SkillCaster caster;

    [Header("Two Panels")]
    public PanelSlot panelA;
    public PanelSlot panelB;

    [Header("Button Prefab")]
    public SkillButton skillButtonPrefab;

    private readonly List<GameObject> spawned = new();

    private void Awake()
    {
        if (allyUnit == null) allyUnit = GetComponent<AllyUnit>();
        if (caster == null) caster = GetComponent<SkillCaster>();

        SetPanelActive(panelA, false);
        SetPanelActive(panelB, false);
    }

    public void Show()
    {
        ClearSpawned();

        if (allyUnit == null || caster == null || skillButtonPrefab == null)
            return;

        // 取前两个技能（你说固定两个技能，这里直接按顺序）
        SkillData skill0 = allyUnit.skills.Count > 0 ? allyUnit.skills[0] : null;
        SkillData skill1 = allyUnit.skills.Count > 1 ? allyUnit.skills[1] : null;

        SetupPanel(panelA, skill0, $"{allyUnit.displayName} - Skill 1");
        SetupPanel(panelB, skill1, $"{allyUnit.displayName} - Skill 2");
    }

    public void Hide()
    {
        ClearSpawned();
        SetPanelActive(panelA, false);
        SetPanelActive(panelB, false);
    }

    public void Toggle()
    {
        bool anyActive = (panelA.panelRoot != null && panelA.panelRoot.activeSelf) ||
                         (panelB.panelRoot != null && panelB.panelRoot.activeSelf);

        if (anyActive) Hide();
        else Show();
    }

    private void SetupPanel(PanelSlot slot, SkillData skill, string title)
    {
        if (slot == null || slot.panelRoot == null) return;

        // 没有对应技能，就不显示这个面板
        if (skill == null)
        {
            SetPanelActive(slot, false);
            return;
        }

        SetPanelActive(slot, true);

        if (slot.titleText != null)
            slot.titleText.text = title;

        if (slot.buttonContainer == null) return;

        // 每个面板只生成 1 个按钮
        var btn = Instantiate(skillButtonPrefab, slot.buttonContainer);
        btn.Bind(allyUnit, skill);   // 用我之前给你的“自动同步角色”的 SkillButton
        btn.RefreshUI();

        spawned.Add(btn.gameObject);
    }

    private void SetPanelActive(PanelSlot slot, bool active)
    {
        if (slot != null && slot.panelRoot != null)
            slot.panelRoot.SetActive(active);
    }

    private void ClearSpawned()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawned.Clear();
    }
}