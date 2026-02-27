using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private GameObject pauseInstance;
    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public void LoadMenuScene()
    {
        Debug.Log("Returning to MainMenu");
        SceneManager.LoadScene("00_MainMenu");
    }
    public void LoadGameScene()
    {
        Debug.Log("Loading MainMap");
        SceneManager.LoadScene("01_MainMap");
    }

    public void QuitGame() 
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

}
