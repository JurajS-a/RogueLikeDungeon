using UnityEngine;

// Soba kao pravokutnik na gridu. Nije MonoBehaviour - soba je
// matematicki objekt generatora, ne objekt u sceni.
public class Room
{
    public RectInt Bounds;

    public Room(RectInt bounds)
    {
        Bounds = bounds;
    }

    public Vector2Int Center => new Vector2Int(
        Bounds.x + Bounds.width / 2,
        Bounds.y + Bounds.height / 2);
}