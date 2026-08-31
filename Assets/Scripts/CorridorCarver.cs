using System.Collections.Generic;
using UnityEngine;

// Vrste plocica u logickoj matrici dungeona.
public enum TileType
{
    Wall,
    Floor
}

// Korak 4: pretvaranje soba i bridova grafa u matricu plocica.
// Uz matricu vraca i celije svakog hodnika, sto koristi postavljanje zamki.
public static class CorridorCarver
{
    public static TileType[,] BuildMap(
        int width, int height,
        List<Room> rooms,
        List<Edge> edges,
        System.Random rng,
        out List<List<Vector2Int>> corridors)
    {
        // Default vrijednost enuma je Wall, pa je matrica vec puna zida
        var map = new TileType[width, height];

        foreach (var room in rooms)
            CarveRoom(map, room);

        corridors = new List<List<Vector2Int>>();
        foreach (var e in edges)
        {
            var cells = new List<Vector2Int>();
            CarveCorridor(map, rooms[e.A].Center, rooms[e.B].Center, rng, cells);
            corridors.Add(cells);
        }

        return map;
    }

    private static void CarveRoom(TileType[,] map, Room room)
    {
        var b = room.Bounds;
        for (int x = b.x; x < b.x + b.width; x++)
            for (int y = b.y; y < b.y + b.height; y++)
                map[x, y] = TileType.Floor;
    }

    // Hodnik u L-obliku: najkraci put po Manhattanskoj metrici s jednim
    // zavojem. Orijentacija se bira nasumicno radi raznolikosti.
    private static void CarveCorridor(
        TileType[,] map, Vector2Int from, Vector2Int to,
        System.Random rng, List<Vector2Int> cells)
    {
        if (rng.Next(2) == 0)
        {
            CarveHorizontal(map, from.x, to.x, from.y, cells);
            CarveVertical(map, from.y, to.y, to.x, cells);
        }
        else
        {
            CarveVertical(map, from.y, to.y, from.x, cells);
            CarveHorizontal(map, from.x, to.x, to.y, cells);
        }
    }

    private static void CarveHorizontal(
        TileType[,] map, int x1, int x2, int y, List<Vector2Int> cells)
    {
        for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
        {
            map[x, y] = TileType.Floor;
            cells.Add(new Vector2Int(x, y));
        }
    }

    private static void CarveVertical(
        TileType[,] map, int y1, int y2, int x, List<Vector2Int> cells)
    {
        for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
        {
            map[x, y] = TileType.Floor;
            cells.Add(new Vector2Int(x, y));
        }
    }
}