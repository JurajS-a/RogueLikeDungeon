using UnityEngine;

// Novcic: na dodir igraca uvecava brojac i nestaje.
public class Coin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null)
            GameManager.Instance.AddCoin();

        Destroy(gameObject);
    }
}