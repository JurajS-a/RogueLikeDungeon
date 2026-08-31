using UnityEngine;

// Animacija hodanja iz sheeta 4x4: stupci su smjerovi, redci okviri koraka.
// Smjer se cita iz brzine Rigidbodyja, pa je koriste i igrac i neprijatelji.
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterAnimator : MonoBehaviour
{
    [Header("Okviri (16 komada)")]
    public Sprite[] walkSheet = new Sprite[16];
    public int columns = 4;
    public float framesPerSecond = 8f;

    [Header("Raspored stupaca")]
    public int colDown = 0;
    public int colUp = 1;
    public int colRight = 2;
    public int colLeft = 3;

    [Tooltip("Ispod ove brzine lik se smatra nepomicnim")]
    public float moveThreshold = 0.05f;

    [Header("Smrt")]
    public Sprite deadSprite;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private int column;
    private float timer;
    private int frame;
    private bool dead;

    // Zamrzava animaciju na mrtvoj pozi do nove igre
    public void SetDead(bool value)
    {
        dead = value;
        if (dead && deadSprite != null)
            sr.sprite = deadSprite;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        column = colDown;
    }

    private void Update()
    {
        if (dead) return;

        Vector2 v = rb != null ? rb.linearVelocity : Vector2.zero;

        // Mirovanje: prvi okvir, ali se zadrzava zadnji smjer
        if (v.magnitude < moveThreshold)
        {
            frame = 0;
            timer = 0f;
            Show();
            return;
        }

        // Dominantna os odreduje smjer; kod dijagonale prednost ima vodoravni
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            column = v.x > 0f ? colRight : colLeft;
        else
            column = v.y > 0f ? colUp : colDown;

        timer += Time.deltaTime;
        int rows = Mathf.Max(1, walkSheet.Length / Mathf.Max(1, columns));
        if (timer >= 1f / Mathf.Max(0.01f, framesPerSecond))
        {
            timer = 0f;
            frame = (frame + 1) % rows;
        }

        Show();
    }

    private void Show()
    {
        if (walkSheet == null || walkSheet.Length == 0) return;

        int index = frame * columns + column;
        if (index >= 0 && index < walkSheet.Length && walkSheet[index] != null)
            sr.sprite = walkSheet[index];
    }
}