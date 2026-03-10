using UnityEngine;

public class VegetationScatter : MonoBehaviour
{
    public GameObject[] prefabs;
    public int count = 100;
    public float areaSize = 20f;

    [Header("¹Ì¶¨³¯Ïò")]
    public Vector3 fixedRotation = new Vector3(0, 0, 0);

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = transform.position + new Vector3(
                Random.Range(-areaSize, areaSize),
                10f,
                Random.Range(-areaSize, areaSize)
            );

            RaycastHit hit;
            if (Physics.Raycast(pos, Vector3.down, out hit, 20f))
            {
                Quaternion rot = Quaternion.Euler(fixedRotation);

                GameObject obj = Instantiate(
                    prefabs[Random.Range(0, prefabs.Length)],
                    hit.point,
                    rot
                );

                obj.transform.localScale *= Random.Range(0.8f, 1.2f);
            }
        }
    }
}