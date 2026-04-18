using UnityEngine;

public class BreathingCamera : MonoBehaviour
{
    [Header("呼吸幅度")]
    public float amplitudeX = 0.03f;
    public float amplitudeY = 0.04f;
    public float amplitudeZ = 0.01f;

    [Header("呼吸速度")]
    public float frequency = 1.2f;

    [Header("平滑程度")]
    public float smoothSpeed = 5f;

    [Header("是否启用呼吸")]
    public bool breathingEnabled = true;

    private Vector3 basePosition;
    private float timeOffset;

    private void Start()
    {
        basePosition = transform.position;
        timeOffset = Random.Range(0f, 100f);
    }

    private void LateUpdate()
    {
        if (!breathingEnabled) return;

        float t = Time.time + timeOffset;

        Vector3 offset = new Vector3(
            Mathf.Sin(t * frequency) * amplitudeX,
            Mathf.Sin(t * frequency * 1.2f) * amplitudeY,
            Mathf.Sin(t * frequency * 0.8f) * amplitudeZ
        );

        Vector3 targetPos = basePosition + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }

    public void SetBasePosition(Vector3 newBasePosition)
    {
        basePosition = newBasePosition;
    }

    public void SnapBaseToCurrentPosition()
    {
        basePosition = transform.position;
    }

    public void SetBreathingEnabled(bool enabled)
    {
        breathingEnabled = enabled;
    }
}