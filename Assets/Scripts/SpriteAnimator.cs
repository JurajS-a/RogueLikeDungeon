using UnityEngine;

// Petljasta animacija niza okvira, za predmete bez smjera i stanja.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 8f;

    private SpriteRenderer sr;
    private float timer;
    private int frame;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / Mathf.Max(0.01f, framesPerSecond))
        {
            timer = 0f;
            frame = (frame + 1) % frames.Length;
        }

        if (frames[frame] != null) sr.sprite = frames[frame];
    }
}