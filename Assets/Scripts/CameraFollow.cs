using UnityEngine;

// Kamera glatko prati igraca i moze se zatresti na udarac.
// LateUpdate se izvrsava nakon pomaka igraca, pa kamera ne kasni frame.
public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Range(0.01f, 1f)]
    public float smoothness = 0.15f;

    private Vector3 velocity;
    private Vector3 basePosition;
    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;

    private void Start()
    {
        basePosition = transform.position;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        var desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        basePosition = Vector3.SmoothDamp(basePosition, desired, ref velocity, smoothness);

        // Tresnja se racuna odvojeno i pribraja na kraju, inace bi
        // SmoothDamp gonio vlastiti sum
        Vector3 shake = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;

            float damper = shakeDuration <= 0f ? 0f : Mathf.Clamp01(shakeTimer / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * (shakeMagnitude * damper);
            shake = new Vector3(offset.x, offset.y, 0f);
        }

        transform.position = basePosition + shake;
    }
}