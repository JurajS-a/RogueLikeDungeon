using UnityEngine;
using UnityEngine.InputSystem;

// Kretanje igraca u 8 smjerova preko Rigidbody2D fizike,
// cime kolizija sa zidovima radi bez rucnih provjera.
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 8f;

    private Rigidbody2D rb;
    private Vector2 input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Ulaz se cita svaki frame da se pritisak ne propusti
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);

        // Bez normalizacije bi dijagonala bila oko 41% brza
        input = new Vector2(x, y).normalized;
    }

    // Primjena na fiziku ide u koraku fizikalne simulacije
    private void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }
}