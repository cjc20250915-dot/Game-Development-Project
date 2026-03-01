using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionController : MonoBehaviour
{
    public Material transitionMat;
    public float transitionTime = 1f;
    public float minWaitTime = 1.5f;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadSceneWithTransition(string sceneName)
    {
        StartCoroutine(Transition(sceneName));
    }

    IEnumerator Transition(string sceneName)
    {
        yield return StartCoroutine(AnimateCircle(-0.2f, 2f));

        yield return new WaitForSecondsRealtime(minWaitTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        yield return StartCoroutine(AnimateCircle(2f, -0.2f));
    }

    IEnumerator AnimateCircle(float from, float to)
    {
        float t = 0;
        while (t < transitionTime)
        {
            t += Time.unscaledDeltaTime;
            float value = Mathf.Lerp(from, to, t / transitionTime);
            transitionMat.SetFloat("_CircleRate", value);
            yield return null;
        }
    }
}