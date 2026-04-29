using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleResultHandler : MonoBehaviour
{
    [Header("??????")]
    public string mapSceneName = "01_MainMap";

    [SerializeField] private TransitionController transitionController;

    private bool resultHandled;

    private void Awake()
    {
        if (transitionController == null)
            transitionController = FindFirstObjectByType<TransitionController>();
    }

    public void OnBattleWin()
    {
        if (resultHandled) return;
        resultHandled = true;

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.CompleteCurrentNodeAsWin();
        }

        ReturnToMap();
    }

    public void OnBattleLose()
    {
        if (resultHandled) return;
        resultHandled = true;

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.CompleteCurrentNodeAsLose();
        }

        ReturnToMap();
    }

    private void ReturnToMap()
    {
        if (transitionController == null)
            transitionController = FindFirstObjectByType<TransitionController>();

        if (transitionController != null)
        {
            transitionController.LoadSceneWithTransition(mapSceneName);
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }
}
