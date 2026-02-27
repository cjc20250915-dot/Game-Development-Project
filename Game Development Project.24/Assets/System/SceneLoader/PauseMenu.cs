using UnityEngine;
using UnityEngine.UI;
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

        Time.timeScale = isPaused ? 0 : 1; // Pause or resume the game
    }

    public void OnClickYes() 
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("00_MainMenu"); // Load the main menu scene
    }

    public void OnClickNo() 
    {
        TogglePopup(); // Close the pause menu
    }
}
