using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image hpBar;
    [SerializeField] private Image hpBarEffect;
    [SerializeField] private float effectDelay = 0.2f;
    [SerializeField] private float effectLerpSpeed = 2f;

    private EnemyUnit boundEnemy;
    private Coroutine effectCoroutine;

    public void BindEnemy(EnemyUnit enemy)
    {
        if (boundEnemy != null)
            boundEnemy.OnHPChanged -= UpdateHP;

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
            boundEnemy.OnHPChanged -= UpdateHP;
    }

    private void UpdateHP(int current, int max)
    {
        float value = max > 0 ? (float)current / max : 0f;

        if (hpBar != null)
            hpBar.fillAmount = value;

        if (hpBarEffect == null) return;

        if (hpBarEffect.fillAmount <= value)
        {
            hpBarEffect.fillAmount = value;
        }
        else
        {
            if (effectCoroutine != null)
                StopCoroutine(effectCoroutine);

            effectCoroutine = StartCoroutine(AnimateEffect(value));
        }
    }

    private IEnumerator AnimateEffect(float target)
    {
        yield return new WaitForSeconds(effectDelay);

        while (hpBarEffect != null && hpBarEffect.fillAmount > target)
        {
            hpBarEffect.fillAmount = Mathf.MoveTowards(
                hpBarEffect.fillAmount,
                target,
                effectLerpSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (hpBarEffect != null)
            hpBarEffect.fillAmount = target;
    }
}