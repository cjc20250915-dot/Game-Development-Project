using UnityEngine;
using System.Collections;

public class PlayerHealthBarBinder : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private AllySlotBoard allySlotBoard;

    [Header("Bind Target")]
    [SerializeField] private bool bindSlotA = true;

    private HealthBarUI healthBarUI;
    private HeartHealthBarUI heartHealthBarUI;
    private AllyUnit currentBoundAlly;
    private Coroutine delayedBindCoroutine;
    private Coroutine retryBindCoroutine;

    private void Awake()
    {
        healthBarUI = GetComponent<HealthBarUI>();
        heartHealthBarUI = GetComponent<HeartHealthBarUI>();
    }

    private void OnEnable()
    {
        AutoFindBoardIfNeeded();

        if (allySlotBoard != null)
            allySlotBoard.OnSlotsChanged += RefreshBinding;

        if (delayedBindCoroutine != null)
            StopCoroutine(delayedBindCoroutine);

        delayedBindCoroutine = StartCoroutine(RefreshBindingNextFrame());

        if (retryBindCoroutine != null)
            StopCoroutine(retryBindCoroutine);
        retryBindCoroutine = StartCoroutine(RetryBindRoutine());
    }

    private void OnDisable()
    {
        if (delayedBindCoroutine != null)
        {
            StopCoroutine(delayedBindCoroutine);
            delayedBindCoroutine = null;
        }

        if (retryBindCoroutine != null)
        {
            StopCoroutine(retryBindCoroutine);
            retryBindCoroutine = null;
        }

        if (allySlotBoard != null)
            allySlotBoard.OnSlotsChanged -= RefreshBinding;
    }

    private IEnumerator RefreshBindingNextFrame()
    {
        // 等待一帧，确保 AllySlotBoard.Start() 先完成初始生成
        yield return null;
        RefreshBinding();
        delayedBindCoroutine = null;
    }

    public void RefreshBinding()
    {
        if (healthBarUI == null && heartHealthBarUI == null) return;
        AutoFindBoardIfNeeded();

        AllyUnit target = ResolveTargetAlly();
        if (target == currentBoundAlly) return;

        currentBoundAlly = target;

        if (healthBarUI != null)
            healthBarUI.BindAlly(currentBoundAlly);

        if (heartHealthBarUI != null)
            heartHealthBarUI.BindAlly(currentBoundAlly);
    }

    private AllyUnit ResolveTargetAlly()
    {
        if (allySlotBoard == null) return null;
        return bindSlotA ? allySlotBoard.SlotA : allySlotBoard.SlotB;
    }

    private IEnumerator RetryBindRoutine()
    {
        // 某些启动顺序下，角色会在数帧后才生成，做短时间重试更稳。
        const int maxRetryFrames = 120;
        for (int i = 0; i < maxRetryFrames; i++)
        {
            if (currentBoundAlly != null)
            {
                retryBindCoroutine = null;
                yield break;
            }

            RefreshBinding();
            yield return null;
        }

        retryBindCoroutine = null;
    }

    private void AutoFindBoardIfNeeded()
    {
        if (allySlotBoard == null)
            allySlotBoard = FindFirstObjectByType<AllySlotBoard>();
    }
}
