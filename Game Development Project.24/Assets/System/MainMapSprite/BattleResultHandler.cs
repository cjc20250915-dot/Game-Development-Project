using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleResultHandler : MonoBehaviour
{
    [Header("??????")]
    public string mapSceneName = "01_MainMap";

    public void OnBattleWin()
    {
        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.CompleteCurrentNodeAsWin();
        }

        SceneManager.LoadScene(mapSceneName);
    }

    public void OnBattleLose()
    {
        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.CompleteCurrentNodeAsLose();
        }

        SceneManager.LoadScene(mapSceneName);
    }
}
