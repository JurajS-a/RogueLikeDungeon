using System.Collections.Generic;
using UnityEngine;

// Orkestrator generatora. Drzi parametre i redom poziva korake:
// sobe -> graf -> MST + petlje -> matrica plocica -> tilemap.
// Nakon toga razmjesta igraca, izlaz, sadrzaj soba i bossa.
//
// Sva slucajnost generacije ide kroz jedan seedani System.Random,
// pa isti seed uvijek daje identican dungeon.
public class DungeonGenerator : MonoBehaviour
{
    [Header("Velicina grida (u plocicama)")]
    public int gridWidth = 60;
    public int gridHeight = 40;

    [Header("Sobe")]
    public int targetRoomCount = 10;
    public int minRoomSize = 5;
    public int maxRoomSize = 12;

    [Tooltip("Minimalni razmak izmedu soba, u plocicama")]
    public int roomPadding = 2;

    [Header("Povezanost")]
    [Tooltip("Udio dodatnih bridova (petlji) u odnosu na broj soba. 0 = cisto stablo")]
    [Range(0f, 0.5f)]
    public float extraEdgeFraction = 0.15f;

    [Header("Seed")]
    public int seed = 12345;
    public bool useRandomSeed = true;

    [Header("Prezentacija")]
    public TilemapPainter painter;

    [Header("Igrac i izlaz")]
    public Transform player;
    public Transform exit;

    [Header("Spawnanje")]
    public GameObject coinPrefab;
    public int minCoins = 3;
    public int maxCoins = 6;

    public GameObject enemyPrefab;
    public int minEnemies = 2;
    public int maxEnemies = 5;

    [Tooltip("Napitak koji se postavlja u sobu s izlazom")]
    public GameObject healthPotionPrefab;

    [Header("Zamke")]
    [Tooltip("U svaki hodnik postavlja se tocno jedna zamka")]
    public GameObject spikeTrapPrefab;

    [Header("Boss")]
    [Tooltip("Boss ide u najvecu sobu koja nije pocetna ni izlazna")]
    public GameObject bossPrefab;

    [Header("Dekoracije")]
    public DecorationSpec[] decorations;

    [Header("Debug prikaz")]
    public bool drawMap = false;
    public bool drawRooms = false;
    public bool drawGraph = false;
    public bool drawCandidateEdges = false;

    private List<Room> rooms = new List<Room>();
    private List<Edge> allEdges = new List<Edge>();
    private List<Edge> mstEdges = new List<Edge>();
    private List<Edge> extraEdges = new List<Edge>();
    private List<List<Vector2Int>> corridors = new List<List<Vector2Int>>();
    private TileType[,] map;
    private int exitRoomIndex = -1;
    private int bossRoomIndex = -1;
    private Transform spawnedContainer;

    public TileType[,] Map => map;
    public IReadOnlyList<Room> Rooms => rooms;

    private void Start()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (useRandomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);

        var rng = new System.Random(seed);

        // Korak 1: sobe
        rooms = RoomPlacer.PlaceRooms(gridWidth, gridHeight, targetRoomCount,
                                      minRoomSize, maxRoomSize, roomPadding, rng);

        // Korak 2: potpuni graf nad centrima soba
        allEdges = GraphBuilder.BuildCompleteGraph(rooms);

        // Korak 3: MST + dodatni bridovi za petlje
        mstEdges = MstSolver.Solve(rooms.Count, allEdges);
        extraEdges = PickExtraEdges(rng);

        // Korak 4: matrica plocica s hodnicima
        var corridorEdges = new List<Edge>(mstEdges);
        corridorEdges.AddRange(extraEdges);
        map = CorridorCarver.BuildMap(gridWidth, gridHeight, rooms, corridorEdges, rng,
                                      out corridors);

        // Korak 5: prikaz
        if (painter != null)
            painter.Paint(map, rooms);

        PlacePlayer();
        PlaceExit(corridorEdges);

        bossRoomIndex = ChooseBossRoom();
        SpawnObjects(rng);
    }

    // Bridovi izvan MST-a, uzeti od najkracih - lokalni precaci umjesto
    // hodnika preko cijele mape
    private List<Edge> PickExtraEdges(System.Random rng)
    {
        var result = new List<Edge>();

        int desired = Mathf.RoundToInt(extraEdgeFraction * rooms.Count);
        if (desired == 0) return result;

        var used = new HashSet<(int, int)>();
        foreach (var e in mstEdges)
            used.Add((Mathf.Min(e.A, e.B), Mathf.Max(e.A, e.B)));

        var candidates = new List<Edge>();
        foreach (var e in allEdges)
        {
            var key = (Mathf.Min(e.A, e.B), Mathf.Max(e.A, e.B));
            if (!used.Contains(key))
                candidates.Add(e);
        }
        candidates.Sort((x, y) => x.Weight.CompareTo(y.Weight));

        foreach (var e in candidates)
        {
            if (result.Count >= desired) break;
            if (rng.NextDouble() < 0.5)
                result.Add(e);
        }

        return result;
    }

    private void PlacePlayer()
    {
        if (player == null || rooms.Count == 0) return;

        var c = rooms[0].Center;
        player.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
    }

    // Izlaz ide u sobu najudaljeniju od pocetne, mjereno BFS-om po grafu
    // (broj soba na putu, ne zracna linija)
    private void PlaceExit(List<Edge> corridorEdges)
    {
        exitRoomIndex = -1;
        if (exit == null || rooms.Count <= 1) return;

        var distances = GraphSearch.BfsDistances(0, rooms.Count, corridorEdges);
        exitRoomIndex = GraphSearch.FarthestNode(distances);

        var c = rooms[exitRoomIndex].Center;
        exit.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
    }

    // Najveca soba po povrsini, bez pocetne (borba bi pocela odmah)
    // i izlazne (barikade ne smiju zatvoriti izlaz)
    private int ChooseBossRoom()
    {
        int best = -1;
        int bestArea = 0;

        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == 0 || i == exitRoomIndex) continue;

            int area = rooms[i].Bounds.width * rooms[i].Bounds.height;
            if (area > bestArea)
            {
                bestArea = area;
                best = i;
            }
        }

        return best;
    }

    // ---------- razmjestaj sadrzaja ----------

    private void SpawnObjects(System.Random rng)
    {
        if (spawnedContainer == null)
            spawnedContainer = new GameObject("Spawned").transform;

        // Ostaci prethodne razine
        for (int i = spawnedContainer.childCount - 1; i >= 0; i--)
        {
            var child = spawnedContainer.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        if (rooms.Count == 0) return;

        // Zauzete celije; centri soba su rezervirani za igraca i izlaz
        var occupied = new HashSet<Vector2Int>();
        foreach (var r in rooms)
            occupied.Add(r.Center);

        SpawnDecorations(rng, occupied);
        SpawnTraps(rng, occupied);
        SpawnExitPotion(rng, occupied);

        // Boss soba ostaje prazna
        var coinRooms = new List<Room>();
        var enemyRooms = new List<Room>();
        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == bossRoomIndex) continue;
            coinRooms.Add(rooms[i]);
            if (i != 0) enemyRooms.Add(rooms[i]);   // ne u pocetnu sobu
        }

        if (coinPrefab != null && coinRooms.Count > 0)
        {
            int count = rng.Next(minCoins, maxCoins + 1);
            for (int i = 0; i < count; i++)
            {
                var room = coinRooms[rng.Next(coinRooms.Count)];
                Instantiate(coinPrefab, RandomFreeCellInRoom(room, rng, occupied),
                            Quaternion.identity, spawnedContainer);
            }
        }

        if (enemyPrefab != null && enemyRooms.Count > 0)
        {
            int count = rng.Next(minEnemies, maxEnemies + 1);
            for (int i = 0; i < count; i++)
            {
                var room = enemyRooms[rng.Next(enemyRooms.Count)];
                Instantiate(enemyPrefab, RandomFreeCellInRoom(room, rng, occupied),
                            Quaternion.identity, spawnedContainer);
            }
        }

        SpawnBoss();
    }

    // Svaka vrsta dekoracije dobiva svoj broj primjeraka po sobi,
    // uz postivanje zone i zasticenih puteva
    private void SpawnDecorations(System.Random rng, HashSet<Vector2Int> occupied)
    {
        if (decorations == null || decorations.Length == 0) return;

        for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
        {
            if (roomIndex == bossRoomIndex) continue;

            var room = rooms[roomIndex];
            var blocked = RoomDecorator.ProtectedCells(map, room);

            foreach (var spec in decorations)
            {
                if (spec == null || spec.prefab == null) continue;

                int wanted = rng.Next(spec.minPerRoom, spec.maxPerRoom + 1);
                int attempts = wanted * 10;

                while (wanted > 0 && attempts-- > 0)
                {
                    var maybeCell = RandomDecorCell(room, spec.zone, rng);
                    if (maybeCell == null) continue;

                    var cell = maybeCell.Value;
                    if (blocked.Contains(cell) || occupied.Contains(cell)) continue;

                    Instantiate(spec.prefab,
                                new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f),
                                Quaternion.identity, spawnedContainer);

                    occupied.Add(cell);
                    wanted--;
                }
            }
        }
    }

    // Celija prema zoni; null znaci da zona u toj sobi nema valjano mjesto
    private static Vector2Int? RandomDecorCell(Room room, DecorZone zone, System.Random rng)
    {
        var b = room.Bounds;

        switch (zone)
        {
            case DecorZone.Center:
                if (b.width <= 2 || b.height <= 2) return null;
                return new Vector2Int(b.x + 1 + rng.Next(b.width - 2),
                                      b.y + 1 + rng.Next(b.height - 2));

            case DecorZone.Corner:
                return new Vector2Int(
                    rng.Next(2) == 0 ? b.x : b.x + b.width - 1,
                    rng.Next(2) == 0 ? b.y : b.y + b.height - 1);

            case DecorZone.AgainstWall:
                {
                    var cell = new Vector2Int(b.x + rng.Next(b.width),
                                              b.y + rng.Next(b.height));
                    bool onRing = cell.x == b.x || cell.x == b.x + b.width - 1
                               || cell.y == b.y || cell.y == b.y + b.height - 1;
                    return onRing ? cell : (Vector2Int?)null;
                }

            default:
                return new Vector2Int(b.x + rng.Next(b.width),
                                      b.y + rng.Next(b.height));
        }
    }

    // Jedna zamka po hodniku, i to samo na dijelu izvan soba
    private void SpawnTraps(System.Random rng, HashSet<Vector2Int> occupied)
    {
        if (spikeTrapPrefab == null || corridors == null) return;

        var candidates = new List<Vector2Int>();

        foreach (var corridor in corridors)
        {
            candidates.Clear();
            foreach (var cell in corridor)
                if (!IsInsideAnyRoom(cell) && !occupied.Contains(cell))
                    candidates.Add(cell);

            if (candidates.Count == 0) continue;

            var chosen = candidates[rng.Next(candidates.Count)];
            Instantiate(spikeTrapPrefab,
                        new Vector3(chosen.x + 0.5f, chosen.y + 0.5f, 0f),
                        Quaternion.identity, spawnedContainer);
            occupied.Add(chosen);
        }
    }

    private void SpawnExitPotion(System.Random rng, HashSet<Vector2Int> occupied)
    {
        if (healthPotionPrefab == null) return;
        if (exitRoomIndex < 0 || exitRoomIndex >= rooms.Count) return;

        var position = RandomFreeCellInRoom(rooms[exitRoomIndex], rng, occupied);
        Instantiate(healthPotionPrefab, position, Quaternion.identity, spawnedContainer);
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null || bossRoomIndex < 0) return;

        var room = rooms[bossRoomIndex];
        var c = room.Center;

        var go = Instantiate(bossPrefab,
                             new Vector3(c.x + 0.5f, c.y + 0.5f, 0f),
                             Quaternion.identity, spawnedContainer);

        var boss = go.GetComponent<Boss>();
        if (boss != null)
            boss.Setup(room.Bounds, RoomDecorator.FindDoorCells(map, room), coinPrefab);
    }

    private bool IsInsideAnyRoom(Vector2Int cell)
    {
        foreach (var room in rooms)
            if (room.Bounds.Contains(cell)) return true;
        return false;
    }

    // Slobodna celija u sobi; nakon niza neuspjeha vraca centar
    private static Vector3 RandomFreeCellInRoom(
        Room room, System.Random rng, HashSet<Vector2Int> occupied)
    {
        var b = room.Bounds;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            var cell = new Vector2Int(b.x + rng.Next(b.width),
                                      b.y + rng.Next(b.height));
            if (occupied.Contains(cell)) continue;

            occupied.Add(cell);
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        return new Vector3(room.Center.x + 0.5f, room.Center.y + 0.5f, 0f);
    }

    // ---------- debug vizualizacija u editoru ----------

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(
            new Vector3(gridWidth / 2f, gridHeight / 2f, 0f),
            new Vector3(gridWidth, gridHeight, 0f));

        if (drawMap && map != null)
        {
            Gizmos.color = new Color(0.45f, 0.45f, 0.5f, 0.9f);
            for (int x = 0; x < map.GetLength(0); x++)
                for (int y = 0; y < map.GetLength(1); y++)
                    if (map[x, y] == TileType.Floor)
                        Gizmos.DrawCube(new Vector3(x + 0.5f, y + 0.5f, 0f),
                                        new Vector3(0.95f, 0.95f, 0f));
        }

        if (rooms == null || rooms.Count == 0) return;

        if (drawCandidateEdges && allEdges != null)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.06f);
            foreach (var e in allEdges) DrawEdge(e);
        }

        if (drawGraph)
        {
            Gizmos.color = Color.yellow;
            if (mstEdges != null) foreach (var e in mstEdges) DrawEdge(e);

            Gizmos.color = Color.cyan;
            if (extraEdges != null) foreach (var e in extraEdges) DrawEdge(e);

            Gizmos.color = Color.white;
            foreach (var room in rooms) Gizmos.DrawSphere(ToWorld(room.Center), 0.4f);
        }

        if (drawRooms)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.6f, 0.45f);
            foreach (var room in rooms)
            {
                var b = room.Bounds;
                Gizmos.DrawCube(
                    new Vector3(b.x + b.width / 2f, b.y + b.height / 2f, 0f),
                    new Vector3(b.width, b.height, 0f));
            }
        }
    }

    private void DrawEdge(Edge e)
        => Gizmos.DrawLine(ToWorld(rooms[e.A].Center), ToWorld(rooms[e.B].Center));

    private static Vector3 ToWorld(Vector2Int gridPos)
        => new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f);
}