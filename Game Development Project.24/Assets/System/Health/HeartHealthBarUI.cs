using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 心形血条：血量从数组<strong>前面</strong>往后面填；缺血时<strong>后面</strong>的心先空（例：50/100 → 前两颗满、第三颗半、后两颗空）。
/// </summary>
public class HeartHealthBarUI : MonoBehaviour
{
    [Header("Heart Setup (Front = Fills First, Back = Empties First)")]
    [SerializeField] private Image[] hearts;
    [SerializeField] private Image[] heartEffects;
    [Tooltip("与 hearts 一一对应：每格心的父物体。当前 maxHP 用不到该槽时，整格隐藏（不占用布局）。")]
    [SerializeField] private GameObject[] heartSlotParents;

    [Header("Rule")]
    [SerializeField] private int hpPerHeart = 20;
    [SerializeField] private bool depleteFromTop = true;

    [Header("Damage Effect")]
    [SerializeField] private bool effectTrailEnabled = false;
    [SerializeField] private float effectDelay = 0.2f;
    [SerializeField] private float effectSpeed = 0.5f;

    [Header("Heart Change Pop")]
    [SerializeField] private bool popOnFillChange = true;
    [SerializeField] private float punchScale = 0.18f;
    [SerializeField] private float punchDuration = 0.35f;
    [SerializeField] private int punchVibrato = 6;
    [SerializeField] [Range(0f, 1f)] private float punchElasticity = 0.5f;

    private AllyUnit boundAlly;
    private float[] targetFills;
    private float[] previousFills;
    private Coroutine effectRoutine;
    private bool initializedVisual;

    public void BindAlly(AllyUnit ally)
    {
        if (boundAlly != null)
            boundAlly.OnHPChanged -= UpdateHearts;

        boundAlly = ally;
        EnsureRuntimeBuffers();
        if (previousFills != null)
        {
            for (int i = 0; i < previousFills.Length; i++)
                previousFills[i] = -1f;
        }
        initializedVisual = false;
        StopEffectRoutine();
        KillAllHeartPunchTweens();

        if (boundAlly != null)
        {
            boundAlly.OnHPChanged += UpdateHearts;
            UpdateHearts(boundAlly.currentHP, boundAlly.maxHP);
        }
        else
        {
            SetAllHeartsVisible(false);
        }
    }

    private void OnDestroy()
    {
        if (boundAlly != null)
            boundAlly.OnHPChanged -= UpdateHearts;
        StopEffectRoutine();
        KillAllHeartPunchTweens();
    }

    private void UpdateHearts(int currentHP, int maxHP)
    {
        if (hearts == null || hearts.Length == 0) return;
        if (hpPerHeart <= 0) hpPerHeart = 20;
        EnsureRuntimeBuffers();

        bool instantSync = !initializedVisual;

        int clampedMax = Mathf.Max(0, maxHP);
        int clampedCurrent = Mathf.Clamp(currentHP, 0, clampedMax);
        int requiredHearts = Mathf.CeilToInt(clampedMax / (float)hpPerHeart);
        int activeHearts = Mathf.Min(requiredHearts, hearts.Length);

        int remainingHP = clampedCurrent;

        for (int i = 0; i < hearts.Length; i++)
        {
            bool enabled = i < activeHearts;
            SetHeartSlotParentActive(i, enabled);

            Image heart = hearts[i];
            if (heart == null) continue;

            heart.enabled = enabled;
            if (!enabled)
            {
                heart.fillAmount = 0f;
                if (previousFills != null && i < previousFills.Length)
                    previousFills[i] = -1f;
                Image hiddenEffect = (heartEffects != null && i < heartEffects.Length) ? heartEffects[i] : null;
                if (hiddenEffect != null)
                {
                    hiddenEffect.enabled = false;
                    hiddenEffect.fillAmount = 0f;
                }
                continue;
            }

            ConfigureHeartImage(heart);

            int thisHeartCapacity = GetHeartCapacityForMax(i, clampedMax, hpPerHeart);
            int assigned = Mathf.Min(thisHeartCapacity, remainingHP);
            remainingHP -= assigned;

            float targetFill = thisHeartCapacity > 0
                ? assigned / (float)thisHeartCapacity
                : 0f;

            targetFills[i] = targetFill;
            heart.fillAmount = targetFill;

            if (popOnFillChange && !instantSync)
            {
                float prev = previousFills != null && i < previousFills.Length ? previousFills[i] : -1f;
                if (prev >= 0f && Mathf.Abs(prev - targetFill) > 0.0005f)
                    PlayHeartPop(i);
            }

            previousFills[i] = targetFill;

            Image effectHeart = (heartEffects != null && i < heartEffects.Length) ? heartEffects[i] : null;
            if (effectHeart == null) continue;

            effectHeart.enabled = enabled;
            if (!enabled) continue;

            ConfigureHeartImage(effectHeart);

            if (instantSync || !effectTrailEnabled)
            {
                effectHeart.fillAmount = targetFill;
                continue;
            }

            if (effectHeart.fillAmount < targetFill)
                effectHeart.fillAmount = targetFill;
        }

        if (instantSync || !effectTrailEnabled)
            StopEffectRoutine();
        else
        {
            StopEffectRoutine();
            effectRoutine = StartCoroutine(AnimateAllEffectHearts());
        }

        initializedVisual = true;
    }

    /// <summary>
    /// 第 i 槽在 maxHP 下最多能容纳多少血（最后一颗可能不足 20）。
    /// </summary>
    private static int GetHeartCapacityForMax(int index, int maxHP, int hpPerHeart)
    {
        int start = index * hpPerHeart;
        if (start >= maxHP) return 0;
        return Mathf.Min(hpPerHeart, maxHP - start);
    }

    private IEnumerator AnimateAllEffectHearts()
    {
        yield return new WaitForSeconds(effectDelay);

        const float epsilon = 0.0001f;
        bool needContinue = true;

        while (needContinue)
        {
            needContinue = false;
            for (int i = 0; i < hearts.Length; i++)
            {
                Image effectHeart = (heartEffects != null && i < heartEffects.Length) ? heartEffects[i] : null;
                if (effectHeart == null || !effectHeart.enabled) continue;

                float target = targetFills[i];
                float current = effectHeart.fillAmount;

                if (current > target + epsilon)
                {
                    effectHeart.fillAmount = Mathf.MoveTowards(
                        current,
                        target,
                        Time.deltaTime * Mathf.Max(0.01f, effectSpeed)
                    );
                    needContinue = true;
                }
                else if (current < target - epsilon)
                    effectHeart.fillAmount = target;
            }
            yield return null;
        }

        for (int i = 0; i < hearts.Length; i++)
        {
            Image effectHeart = (heartEffects != null && i < heartEffects.Length) ? heartEffects[i] : null;
            if (effectHeart == null || !effectHeart.enabled) continue;
            effectHeart.fillAmount = targetFills[i];
        }

        effectRoutine = null;
    }

    private void StopEffectRoutine()
    {
        if (effectRoutine == null) return;
        StopCoroutine(effectRoutine);
        effectRoutine = null;
    }

    private void SetAllHeartsVisible(bool visible)
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            SetHeartSlotParentActive(i, visible);
            if (hearts[i] != null)
                hearts[i].enabled = visible;

            if (heartEffects != null && i < heartEffects.Length && heartEffects[i] != null)
                heartEffects[i].enabled = visible;
        }
    }

    private void SetHeartSlotParentActive(int index, bool active)
    {
        if (heartSlotParents == null || index < 0 || index >= heartSlotParents.Length) return;
        GameObject parent = heartSlotParents[index];
        if (parent != null && parent.activeSelf != active)
            parent.SetActive(active);
    }

    private void ConfigureHeartImage(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Vertical;
        image.fillOrigin = depleteFromTop
            ? (int)Image.OriginVertical.Bottom
            : (int)Image.OriginVertical.Top;
        image.fillClockwise = true;
    }

    private void EnsureRuntimeBuffers()
    {
        int count = hearts != null ? hearts.Length : 0;
        if (count <= 0) return;

        if (targetFills == null || targetFills.Length != count)
            targetFills = new float[count];

        if (previousFills == null || previousFills.Length != count)
        {
            previousFills = new float[count];
            for (int i = 0; i < count; i++)
                previousFills[i] = -1f;
        }
    }

    private Transform GetHeartPulseTarget(int index)
    {
        if (heartSlotParents != null && index >= 0 && index < heartSlotParents.Length && heartSlotParents[index] != null)
            return heartSlotParents[index].transform;
        if (hearts != null && index >= 0 && index < hearts.Length && hearts[index] != null)
            return hearts[index].transform;
        return null;
    }

    private void PlayHeartPop(int index)
    {
        Transform t = GetHeartPulseTarget(index);
        if (t == null) return;

        t.DOKill();
        t.localScale = Vector3.one;
        Vector3 punch = new Vector3(punchScale, punchScale, 0f);
        t.DOPunchScale(punch, punchDuration, punchVibrato, punchElasticity)
            .SetUpdate(true);
    }

    private void KillAllHeartPunchTweens()
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
        {
            Transform t = GetHeartPulseTarget(i);
            if (t != null)
                t.DOKill();
        }
    }
}
