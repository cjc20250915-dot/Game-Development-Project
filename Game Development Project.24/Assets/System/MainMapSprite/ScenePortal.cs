using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    
    public string targetSceneName;

    
    public string targetSpawnID;

    
    public bool autoTrigger = true;

    private bool playerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (autoTrigger)
        {
            Teleport();
        }
        else
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
    }

    private void Update()
    {
        if (!autoTrigger && playerInside && Input.GetKeyDown(KeyCode.E))
        {
            Teleport();
        }
    }

    public void Teleport()
    {
        PlayerPrefs.SetString("TargetSpawnID", targetSpawnID);
        SceneManager.LoadScene(targetSceneName);
    }
}