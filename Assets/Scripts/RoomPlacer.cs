using System.Collections.Generic;
using UnityEngine;

// Korak 1: razmjestaj soba nasumicnim postavljanjem s odbacivanjem
// (rejection sampling).
public static class RoomPlacer
{
    public static List<Room> PlaceRooms(
        int gridWidth, int gridHeight,
        int targetRoomCount,
        int minRoomSize, int maxRoomSize,
        int roomPadding,
        System.Random rng)
    {
        var rooms = new List<Room>();

        // Limit pokusaja garantira zavrsetak kad na grid ne stane
        // trazeni broj soba
        int maxAttempts = targetRoomCount * 25;
        int attempts = 0;

        while (rooms.Count < targetRoomCount && attempts < maxAttempts)
        {
            attempts++;

            int w = rng.Next(minRoomSize, maxRoomSize + 1);
            int h = rng.Next(minRoomSize, maxRoomSize + 1);
            int x = rng.Next(1, gridWidth - w - 1);
            int y = rng.Next(1, gridHeight - h - 1);

            var candidate = new RectInt(x, y, w, h);

            if (!OverlapsAny(candidate, rooms, roomPadding))
                rooms.Add(new Room(candidate));
        }

        return rooms;
    }

    // Kandidat se prije provjere prosiruje za padding, cime se osigurava
    // minimalni razmak izmedu soba
    private static bool OverlapsAny(RectInt candidate, List<Room> rooms, int padding)
    {
        var padded = new RectInt(
            candidate.x - padding,
            candidate.y - padding,
            candidate.width + 2 * padding,
            candidate.height + 2 * padding);

        foreach (var room in rooms)
            if (padded.Overlaps(room.Bounds))
                return true;

        return false;
    }
}