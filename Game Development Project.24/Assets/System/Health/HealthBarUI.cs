using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    public Image hpBar;
    public Image hpBarEffect;

    float targetFill;
    private EnemyUnit boundEnemy;

    public void BindEnemy(EnemyUnit enemy)
    {
        if (boundEnemy != null)
        {
            boundEnemy.OnHPChanged -= UpdateHP;
        }

        boundEnemy = enemy;

        if (boundEnemy != null)
        {
            boundEnemy.OnHPChanged += UpdateHP;
            UpdateHP(boundEnemy.currentHP, boundEnemy.maxHP);
        }
    }

    private void OnDestroy()
    {
        if (boundEnemy != null)
        {
            boundEnemy.OnHPChanged -= UpdateHP;
        }
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