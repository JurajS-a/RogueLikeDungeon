using UnityEngine;

// Neprijatelj koji nasumicno luta: drzi smjer neko vrijeme, pa bira novi.
// Sporiji je od igraca, a dodir oduzima zivot.
public class EnemyWander : MonoBehaviour
{
    public float speed = 3.5f;

    [Tooltip("Min i max sekundi prije promjene smjera")]
    public Vector2 directionChangeInterval = new Vector2(0.8f, 2.5f);

    private Rigidbody2D rb;
    private Vector2 direction;
    private float timer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        PickNewDirection();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) PickNewDirection();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.PlayerHit();
            return;
        }

        // Udarac u zid: odmah novi smjer, inace bi se gurao u prepreku
        PickNewDirection();
    }

    // Nasumicni kut na kruznici. Ovdje se koristi UnityEngine.Random:
    // generacija razine mora biti deterministicna, ponasanje protivnika ne.
    private void PickNewDirection()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        timer = Random.Range(directionChangeInterval.x, directionChangeInterval.y);
    }
}