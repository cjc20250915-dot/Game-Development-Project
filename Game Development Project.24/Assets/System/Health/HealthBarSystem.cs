using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    public Image hpBar;        // 鲜红血条
    public Image hpBarEffect;  // 深红拖尾

    float targetFill;

    public void BindEnemy(EnemyUnit enemy)
    {
        enemy.OnHPChanged += UpdateHP;
        UpdateHP(enemy.currentHP, enemy.maxHP);
    }
    void UpdateHP(int current, int max)
    {
        targetFill = (float)current / max;

        hpBar.fillAmount = targetFill;

        // 如果拖尾条比当前小，说明是回血
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