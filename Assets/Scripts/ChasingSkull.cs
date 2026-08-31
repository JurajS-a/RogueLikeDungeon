using UnityEngine;

// Lubanja koju priziva boss: ide ravno prema igracu, ali sporije od njega.
[RequireComponent(typeof(Rigidbody2D))]
public class ChasingSkull : MonoBehaviour
{
    [Tooltip("Udio brzine igraca; 0.8 znaci 20% sporije")]
    [Range(0.1f, 1f)]
    public float speedFactor = 0.8f;

    public float fallbackSpeed = 6f;

    private Rigidbody2D rb;
    private Transform target;
    private float speed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Brzina se izvodi iz igraceve, pa odnos ostaje isti i ako se
    // moveSpeed kasnije promijeni
    private void Start()
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            target = pm.transform;
            speed = pm.moveSpeed * speedFactor;
        }
        else
        {
            speed = fallbackSpeed * speedFactor;
        }
    }

    private void FixedUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = direction * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerHit();
    }
}