using UnityEngine;
using UnityEngine.Rendering.Universal;

// Svjetlo koje pulsira izmedu dvije jacine, za isticanje predmeta.
[RequireComponent(typeof(Light2D))]
public class PulsingLight : MonoBehaviour
{
    public float minIntensity = 0.35f;
    public float maxIntensity = 1.0f;
    public float speed = 3f;

    private Light2D light2d;

    private void Awake()
    {
        light2d = GetComponent<Light2D>();
    }

    private void Update()
    {
        float t = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
        light2d.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}