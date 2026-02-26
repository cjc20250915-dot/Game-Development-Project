using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        // 面向摄像机
        transform.forward = cam.transform.forward;
    }
}