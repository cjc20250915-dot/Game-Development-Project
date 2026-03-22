using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
   
    public string spawnID;

    private void Start()
    {
        string targetSpawnID = PlayerPrefs.GetString("TargetSpawnID", "");

        if (string.IsNullOrEmpty(targetSpawnID)) return;
        if (targetSpawnID != spawnID) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("ц╩спур╣╫ Player");
            return;
        }

        player.transform.position = transform.position;
        player.transform.rotation = transform.rotation;

        PlayerPrefs.DeleteKey("TargetSpawnID");
    }
}