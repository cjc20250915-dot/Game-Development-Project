using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("¸úËæÆ«ÒÆ")]
    public Vector3 offset = new Vector3(0f, 8f, -8f);

    [Header("¸úËæËÙ¶È")]
    public float followSpeed = 8f;

    [Header("ÊÇ·ñ¸úËæYÖá")]
    public bool followY = false;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;

        if (!followY)
        {
            targetPos.y = transform.position.y;
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }
}