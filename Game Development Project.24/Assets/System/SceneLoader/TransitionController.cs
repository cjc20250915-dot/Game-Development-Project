using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionController : MonoBehaviour
{
    public Material transitionMat;
    public float transitionTime = 1f;
    public float minWaitTime = 1.5f;
    public float destroyDelayAfterLoad = 2f;

    private bool isTransitioning = false;

    void Awake()
    {
        // 强制恢复鼠标与时间状态，避免从暂停菜单切场景后鼠标失效
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        DontDestroyOnLoad(gameObject);
    }

    public void LoadSceneWithTransition(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(Transition(sceneName));
        }
    }

IEnumerator Transition(string sceneName)
{
    isTransitioning = true;

    // 禁用鼠标（转场开始）
    Cursor.visible = false;
    Cursor.lockState = CursorLockMode.Locked;

    yield return StartCoroutine(AnimateCircle(-0.2f, 2f));

    yield return new WaitForSecondsRealtime(minWaitTime);

    AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
    while (!op.isDone)
        yield return null;

    yield return StartCoroutine(AnimateCircle(2f, -0.2f));

    // 转场结束时必须恢复：否则目标场景若没有会重置 Cursor 的对象（例如 Indoor），鼠标会一直锁定/隐藏
    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
    isTransitioning = false;

    Destroy(gameObject, destroyDelayAfterLoad);
}
    IEnumerator AnimateCircle(float from, float to)
    {
        float t = 0f;

        while (t < transitionTime)
        {
            t += Time.unscaledDeltaTime;
            float value = Mathf.Lerp(from, to, t / transitionTime);
            transitionMat.SetFloat("_CircleRate", value);
            yield return null;
        }

        transitionMat.SetFloat("_CircleRate", to);
    }
}