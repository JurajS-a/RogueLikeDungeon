using UnityEngine;

// Izlaz: na dodir igraca generira novu razinu, cime se zatvara
// roguelike petlja.
public class LevelExit : MonoBehaviour
{
    public DungeonGenerator generator;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (generator != null)
            generator.Generate();
    }
}