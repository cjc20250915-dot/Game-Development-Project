using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    public Image hpBar;
    public Image hpBarEffect;

    float targetFill;
    private EnemyUnit boundEnemy;
    private AllyUnit boundAlly;

    public void BindEnemy(EnemyUnit enemy)
    {
        UnbindCurrentTarget();

        boundEnemy = enemy;
        boundAlly = null;

        if (boundEnemy != null)
        {
            boundEnemy.OnHPChanged += UpdateHP;
            UpdateHP(boundEnemy.currentHP, boundEnemy.maxHP);
        }
    }

    public void BindAlly(AllyUnit ally)
    {
        UnbindCurrentTarget();

        boundAlly = ally;
        boundEnemy = null;

        if (boundAlly != null)
        {
            boundAlly.OnHPChanged += UpdateHP;
            UpdateHP(boundAlly.currentHP, boundAlly.maxHP);
        }
    }

    private void OnDestroy()
    {
        UnbindCurrentTarget();
    }

    private void UnbindCurrentTarget()
    {
        if (boundEnemy != null)
            boundEnemy.OnHPChanged -= UpdateHP;

        if (boundAlly != null)
            boundAlly.OnHPChanged -= UpdateHP;
    }

    void UpdateHP(int current, int max)
    {
        targetFill = max > 0 ? (float)current / max : 0f;

        hpBar.fillAmount = targetFill;

        if (hpBarEffect.fillAmount < targetFill)
        {
            hpBarEffect.fillAmount = targetFill;
            return;
        }

        StopAllCoroutines();
        StartCoroutine(DelayEffect());
    }

    IEnumerator DelayEffect()
    {
        yield return new WaitForSeconds(0.2f);

        while (hpBarEffect.fillAmount > targetFill)
        {
            hpBarEffect.fillAmount -= Time.deltaTime * 0.5f;
            yield return null;
        }

        hpBarEffect.fillAmount = targetFill;
    }
}