using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePopup();
        }
    }

    void TogglePopup()
    {
        isPaused = !isPaused;
        pauseMenuUI.SetActive(isPaused);
        Time.timeScale = isPaused ? 0 : 1;
    }

    public void OnClickYes()
    {
        Time.timeScale = 1;
        pauseMenuUI.SetActive(false);
        if (GameRunManager.Instance != null)
            GameRunManager.Instance.AbortUnfinishedBattleIfNeeded();
        SceneManager.LoadScene("00_MainMenu");
    }

    public void OnClickNo()
    {
        TogglePopup();
    }
}