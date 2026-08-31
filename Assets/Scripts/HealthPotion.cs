using UnityEngine;

// Napitak u sobi s izlazom; vraca jedno srce.
// Ako je zdravlje puno, ne trosi se nego ostaje stajati.
public class HealthPotion : MonoBehaviour
{
    public int healAmount = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.AddLife(healAmount))
            Destroy(gameObject);
    }
}