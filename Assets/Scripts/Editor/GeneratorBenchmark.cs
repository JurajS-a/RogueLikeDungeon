using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

// Mjerni alat za poglavlje Eksperimenti.
// Pokrece se iz izbornika Tools > Benchmark generatora i ispisuje
// tablicu rezultata u Console.
//
// Mjeri se samo algoritamski dio (koraci 1-4), bez stvaranja objekata
// u sceni, jer je on predmet analize.
public static class GeneratorBenchmark
{
    private const int Runs = 1000;

    // Parametri moraju odgovarati onima na DungeonGeneratoru u sceni
    private const int GridWidth = 60;
    private const int GridHeight = 40;
    private const int TargetRooms = 10;
    private const int MinRoomSize = 5;
    private const int MaxRoomSize = 12;
    private const int RoomPadding = 2;
    private const float ExtraEdgeFraction = 0.15f;

    [MenuItem("Tools/Benchmark generatora")]
    public static void Run()
    {
        var times = new List<double>(Runs);
        int solvable = 0;
        int totalRooms = 0;
        int fullRoomCount = 0;
        int totalCorridors = 0;
        long memoryBefore, memoryAfter;

        // Zagrijavanje: prvih nekoliko prolaza ukljucuje JIT prevodenje
        for (int i = 0; i < 20; i++) GenerateOnce(i, out _, out _, out _);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        memoryBefore = GC.GetTotalMemory(true);

        var sw = new Stopwatch();

        for (int seed = 0; seed < Runs; seed++)
        {
            sw.Restart();
            var map = GenerateOnce(seed, out var rooms, out var corridorCount, out var mstCount);
            sw.Stop();

            times.Add(sw.Elapsed.TotalMilliseconds);
            totalRooms += rooms.Count;
            if (rooms.Count == TargetRooms) fullRoomCount++;
            totalCorridors += corridorCount;

            if (IsSolvable(map, rooms)) solvable++;
        }

        memoryAfter = GC.GetTotalMemory(false);

        times.Sort();
        double sum = 0;
        foreach (var t in times) sum += t;

        var report = new StringBuilder();
        report.AppendLine("=== BENCHMARK GENERATORA ===");
        report.AppendLine($"Broj generiranja: {Runs}");
        report.AppendLine($"Grid: {GridWidth}x{GridHeight}, ciljani broj soba: {TargetRooms}");
        report.AppendLine();
        report.AppendLine("--- Vrijeme (ms) ---");
        report.AppendLine($"Aritmeticka sredina: {sum / Runs:F4}");
        report.AppendLine($"Medijan:             {times[Runs / 2]:F4}");
        report.AppendLine($"Minimum:             {times[0]:F4}");
        report.AppendLine($"Maksimum:            {times[Runs - 1]:F4}");
        report.AppendLine($"95. percentil:       {times[(int)(Runs * 0.95)]:F4}");
        report.AppendLine();
        report.AppendLine("--- Memorija ---");
        report.AppendLine($"Ukupno alocirano tijekom {Runs} generiranja: " +
                          $"{(memoryAfter - memoryBefore) / 1024.0:F1} kB");
        report.AppendLine($"Prosjecno po generiranju: " +
                          $"{(memoryAfter - memoryBefore) / (double)Runs / 1024.0:F2} kB");
        report.AppendLine($"Matrica plocica (staticki dio): " +
                          $"{GridWidth * GridHeight * sizeof(int) / 1024.0:F1} kB");
        report.AppendLine();
        report.AppendLine("--- Struktura razine ---");
        report.AppendLine($"Prosjecan broj soba: {totalRooms / (double)Runs:F2} / {TargetRooms}");
        report.AppendLine($"Udio razina s punim brojem soba: {100.0 * fullRoomCount / Runs:F1} %");
        report.AppendLine($"Prosjecan broj hodnika: {totalCorridors / (double)Runs:F2}");
        report.AppendLine();
        report.AppendLine("--- Rjesivost ---");
        report.AppendLine($"Prohodnih razina: {solvable} / {Runs} " +
                          $"({100.0 * solvable / Runs:F2} %)");

        Debug.Log(report.ToString());
    }

    // Jedan prolaz kroz algoritamski dio pipelinea
    private static TileType[,] GenerateOnce(int seed, out List<Room> rooms,
                                            out int corridorCount, out int mstCount)
    {
        var rng = new System.Random(seed);

        rooms = RoomPlacer.PlaceRooms(GridWidth, GridHeight, TargetRooms,
                                      MinRoomSize, MaxRoomSize, RoomPadding, rng);

        var allEdges = GraphBuilder.BuildCompleteGraph(rooms);
        var mstEdges = MstSolver.Solve(rooms.Count, allEdges);
        mstCount = mstEdges.Count;

        var edges = new List<Edge>(mstEdges);
        edges.AddRange(PickExtraEdges(rooms.Count, allEdges, mstEdges, rng));

        var map = CorridorCarver.BuildMap(GridWidth, GridHeight, rooms, edges, rng,
                                          out var corridors);
        corridorCount = corridors.Count;
        return map;
    }

    private static List<Edge> PickExtraEdges(int roomCount, List<Edge> allEdges,
                                             List<Edge> mstEdges, System.Random rng)
    {
        var result = new List<Edge>();
        int desired = Mathf.RoundToInt(ExtraEdgeFraction * roomCount);
        if (desired == 0) return result;

        var used = new HashSet<(int, int)>();
        foreach (var e in mstEdges)
            used.Add((Mathf.Min(e.A, e.B), Mathf.Max(e.A, e.B)));

        var candidates = new List<Edge>();
        foreach (var e in allEdges)
            if (!used.Contains((Mathf.Min(e.A, e.B), Mathf.Max(e.A, e.B))))
                candidates.Add(e);

        candidates.Sort((x, y) => x.Weight.CompareTo(y.Weight));

        foreach (var e in candidates)
        {
            if (result.Count >= desired) break;
            if (rng.NextDouble() < 0.5) result.Add(e);
        }

        return result;
    }

    // Rjesivost: pretrazivanjem u sirinu po podnim celijama provjerava se
    // je li iz pocetne sobe dostupan centar svake druge sobe.
    // Time je pokriven i izlaz, koji uvijek lezi u centru neke sobe.
    private static bool IsSolvable(TileType[,] map, List<Room> rooms)
    {
        if (rooms.Count == 0) return false;

        int w = map.GetLength(0);
        int h = map.GetLength(1);
        var visited = new bool[w, h];

        var start = rooms[0].Center;
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        var dx = new[] { 1, -1, 0, 0 };
        var dy = new[] { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            for (int k = 0; k < 4; k++)
            {
                int nx = c.x + dx[k];
                int ny = c.y + dy[k];

                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (visited[nx, ny] || map[nx, ny] != TileType.Floor) continue;

                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        foreach (var room in rooms)
        {
            var c = room.Center;
            if (!visited[c.x, c.y]) return false;
        }

        return true;
    }
}
