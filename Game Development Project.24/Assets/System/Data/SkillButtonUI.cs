using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Refs")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private GameObject darkMask;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;
    [SerializeField] private float fadeDuration = 0.15f;

    private SkillData currentSkill;
    private AllyUnit owner;
    /// <summary>非 null 时点击改为对该 SkillCaster.TryCast（固定技能按钮）。</summary>
    private SkillCaster casterOverride;
    private Coroutine tooltipFadeRoutine;

    public SkillData CurrentSkill => currentSkill;
    public AllyUnit Owner => owner;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (tooltipRoot != null && tooltipCanvasGroup == null)
            tooltipCanvasGroup = tooltipRoot.GetComponent<CanvasGroup>();

        HideTooltipImmediate();
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClickButton);
            button.onClick.AddListener(OnClickButton);
        }
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClickButton);
    }

    public void BindSkill(AllyUnit newOwner, SkillData skill)
    {
        casterOverride = null;
        owner = newOwner;
        currentSkill = skill;

        RefreshVisualState();
    }

    /// <summary>
    /// 固定技能：技能资产来自外部 ScriptableObject，释放时使用指定的 SkillCaster（例如绑在 SlotA 角色上的组件）。
    /// </summary>
    public void BindFixedSkill(SkillCaster caster, SkillData skill)
    {
        casterOverride = caster;
        currentSkill = skill;
        owner = null;

        if (caster != null)
        {
            owner = caster.GetComponentInParent<AllyUnit>();
            if (owner == null)
                owner = caster.GetComponentInChildren<AllyUnit>();
        }

        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        bool hasSkill = currentSkill != null &&
                        (casterOverride != null || owner != null);

        if (button != null)
            button.interactable = hasSkill;

        if (skillNameText != null)
            skillNameText.text = hasSkill ? currentSkill.skillName : "";

        if (tooltipText != null)
            tooltipText.text = hasSkill ? currentSkill.description : "";

        if (darkMask != null)
            darkMask.SetActive(!hasSkill);

        if (!hasSkill)
            HideTooltipImmediate();
    }

    public void ClearSkill()
    {
        casterOverride = null;
        BindSkill(null, null);
    }

    private void OnClickButton()
    {
        if (currentSkill == null)
        {
            Debug.LogWarning("[SkillButtonUI] Skill is null.");
            return;
        }

        SkillCaster caster = casterOverride;

        if (caster == null && owner != null)
        {
            caster = owner.GetComponent<SkillCaster>();
            if (caster == null)
                caster = owner.GetComponentInParent<SkillCaster>();
            if (caster == null)
                caster = owner.GetComponentInChildren<SkillCaster>();
        }

        if (caster == null)
        {
            Debug.LogWarning("[SkillButtonUI] No SkillCaster — bind ally skill or fixed skill caster.");
            return;
        }

        bool success = caster.TryCast(currentSkill);
        Debug.Log($"[SkillButtonUI] Click cast result = {success}, skill = {currentSkill.skillName}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentSkill == null || (owner == null && casterOverride == null))
            return;

        FadeTooltip(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FadeTooltip(false);
    }

    private void FadeTooltip(bool show)
    {
        if (tooltipRoot == null || tooltipCanvasGroup == null)
            return;

        if (tooltipFadeRoutine != null)
            StopCoroutine(tooltipFadeRoutine);

        tooltipFadeRoutine = StartCoroutine(FadeTooltipRoutine(show));
    }

    private IEnumerator FadeTooltipRoutine(bool show)
    {
        if (show)
        {
            tooltipRoot.SetActive(true);
            tooltipCanvasGroup.blocksRaycasts = false;
        }

        float start = tooltipCanvasGroup.alpha;
        float end = show ? 1f : 0f;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = fadeDuration <= 0f ? 1f : time / fadeDuration;
            tooltipCanvasGroup.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        tooltipCanvasGroup.alpha = end;

        if (!show)
        {
            tooltipRoot.SetActive(false);
            tooltipCanvasGroup.blocksRaycasts = false;
        }

        tooltipFadeRoutine = null;
    }

    private void HideTooltipImmediate()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
            tooltipCanvasGroup.blocksRaycasts = false;
        }
    }
}