using UnityEngine;

// Podna zamka s bodljama. Ciklus: mirovanje -> izlazak -> bodlje vani
// -> uvlacenje. Dok su bodlje vani zamka je neprohodna i dodir oduzima
// zivot; dok miruje, preko nje se prelazi.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class SpikeTrap : MonoBehaviour
{
    [Header("Okviri animacije")]
    [Tooltip("0 = zatvoreno, 4 = bodlje vani, ostalo prijelazi")]
    public Sprite[] frames;

    [Header("Trajanja (sekunde)")]
    public float hiddenDuration = 3f;
    public float activeDuration = 2f;
    public float transitionDuration = 0.25f;

    [Tooltip("Nasumicni pomak u ciklusu, da zamke ne pulsiraju u istom ritmu")]
    public bool randomPhase = true;

    private enum Phase { Hidden, Emerging, Active, Retracting }

    private SpriteRenderer sr;
    private BoxCollider2D box;
    private Phase phase = Phase.Hidden;
    private float timer;
    private bool playerInside;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        // Zamka pokriva tocno jednu celiju, neovisno o velicini sprite-a
        box.size = new Vector2(0.9f, 0.9f);
        box.offset = Vector2.zero;

        if (randomPhase)
            timer = Random.Range(0f, hiddenDuration);

        SetPhase(Phase.Hidden, keepTimer: true);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        switch (phase)
        {
            case Phase.Hidden:
                ShowFrame(0);
                if (timer >= hiddenDuration) SetPhase(Phase.Emerging);
                break;

            case Phase.Emerging:
                ShowFrame(1 + Mathf.FloorToInt(Progress() * 3f));
                if (timer >= transitionDuration) SetPhase(Phase.Active);
                break;

            case Phase.Active:
                ShowFrame(4);
                if (playerInside) Hit();
                if (timer >= activeDuration) SetPhase(Phase.Retracting);
                break;

            case Phase.Retracting:
                ShowFrame(5 + Mathf.FloorToInt(Progress() * 3f));
                if (timer >= transitionDuration) SetPhase(Phase.Hidden);
                break;
        }
    }

    private float Progress()
        => transitionDuration <= 0f ? 1f : Mathf.Clamp01(timer / transitionDuration);

    // Cvrst collider samo dok su bodlje vani; inace trigger, pa se
    // preko zamke moze prijeci ali se dodir i dalje biljezi
    private void SetPhase(Phase next, bool keepTimer = false)
    {
        phase = next;
        if (!keepTimer) timer = 0f;

        box.isTrigger = (next != Phase.Active);
    }

    private void ShowFrame(int index)
    {
        if (frames == null || frames.Length == 0) return;
        sr.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }

    private void Hit()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerHit();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) Hit();
    }
}