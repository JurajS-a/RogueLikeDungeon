using System.Collections.Generic;
using UnityEngine;

// Odreduje celije sobe na kojima dekoracija ne smije stajati.
// Stite se najkraci putevi izmedu svih parova vrata te od vrata do
// centra sobe, cime je prohodnost sobe zajamcena i uz prepreke.
public static class RoomDecorator
{
    public static HashSet<Vector2Int> ProtectedCells(TileType[,] map, Room room)
    {
        var doors = FindDoorCells(map, room);
        var blocked = new HashSet<Vector2Int>();

        for (int i = 0; i < doors.Count; i++)
            for (int j = i + 1; j < doors.Count; j++)
                AddLPath(blocked, doors[i], doors[j]);

        foreach (var d in doors)
            AddLPath(blocked, d, room.Center);

        blocked.Add(room.Center);
        return blocked;
    }

    // Vrata: rubna celija sobe ciji je susjed izvan sobe pod - tamo
    // hodnik probija zid.
    public static List<Vector2Int> FindDoorCells(TileType[,] map, Room room)
    {
        var doors = new List<Vector2Int>();
        var b = room.Bounds;
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        for (int x = b.x; x < b.x + b.width; x++)
        {
            if (IsFloor(map, x, b.y - 1, w, h))
                doors.Add(new Vector2Int(x, b.y));
            if (IsFloor(map, x, b.y + b.height, w, h))
                doors.Add(new Vector2Int(x, b.y + b.height - 1));
        }

        for (int y = b.y; y < b.y + b.height; y++)
        {
            if (IsFloor(map, b.x - 1, y, w, h))
                doors.Add(new Vector2Int(b.x, y));
            if (IsFloor(map, b.x + b.width, y, w, h))
                doors.Add(new Vector2Int(b.x + b.width - 1, y));
        }

        return doors;
    }

    private static bool IsFloor(TileType[,] map, int x, int y, int w, int h)
    {
        if (x < 0 || x >= w || y < 0 || y >= h) return false;
        return map[x, y] == TileType.Floor;
    }

    // L-put izmedu dvije celije, ukljucivo obje krajnje tocke
    private static void AddLPath(HashSet<Vector2Int> cells, Vector2Int from, Vector2Int to)
    {
        int step = from.x <= to.x ? 1 : -1;
        for (int x = from.x; x != to.x + step; x += step)
            cells.Add(new Vector2Int(x, from.y));

        step = from.y <= to.y ? 1 : -1;
        for (int y = from.y; y != to.y + step; y += step)
            cells.Add(new Vector2Int(to.x, y));
    }
}