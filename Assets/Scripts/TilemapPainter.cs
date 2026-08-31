using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Korak 5: prezentacijski sloj. Jedina klasa generatora koja ovisi
// o Unity Tilemap sustavu.
//
// Slikanje ide u slojevima:
//   1) pod svuda, zidovi hodnika kao nevidljivi collideri
//   2) room template preko svake sobe
public class TilemapPainter : MonoBehaviour
{
    [Header("Tilemape")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;

    [Header("Osnovni pod")]
    public TileBase floorTile;

    [Header("Room template")]
    public RoomTileSet roomTiles;

    private TileBase invisibleWall;

    public void Paint(TileType[,] map, IReadOnlyList<Room> rooms = null)
    {
        if (floorTilemap == null || wallTilemap == null || floorTile == null)
        {
            Debug.LogWarning("TilemapPainter: nedostaju tilemape ili podna plocica.");
            return;
        }

        if (invisibleWall == null) invisibleWall = MakeInvisibleWallTile();

        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        PaintBase(map);

        if (rooms != null && roomTiles != null && roomTiles.IsUsable)
            foreach (var room in rooms)
                PaintRoomTemplate(map, room);
    }

    // Pod svuda; zidne celije uz pod dobivaju nevidljivu plocicu koja
    // nosi samo koliziju (zidovi hodnika se ne crtaju).
    private void PaintBase(TileType[,] map)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector3Int(x, y, 0);

                if (map[x, y] == TileType.Floor)
                    floorTilemap.SetTile(pos, floorTile);
                else if (TouchesFloor(map, x, y))
                    wallTilemap.SetTile(pos, invisibleWall);
            }
        }
    }

    // Soba se slika neovisno o velicini: pod sa sjenama uz lijevi i donji
    // rub, zidni prsten na celijama tocno oko granica sobe.
    private void PaintRoomTemplate(TileType[,] map, Room room)
    {
        var b = room.Bounds;
        int x0 = b.x, x1 = b.xMax - 1;
        int y0 = b.y, y1 = b.yMax - 1;

        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                bool west = (x == x0);
                bool south = (y == y0);
                bool north = (y == y1);

                TileBase t = roomTiles.floorMid;
                if (west && north) t = roomTiles.Or(roomTiles.floorNW, roomTiles.floorMid);
                else if (west && south) t = roomTiles.Or(roomTiles.floorSW, roomTiles.floorMid);
                else if (west) t = roomTiles.Or(roomTiles.floorW, roomTiles.floorMid);
                else if (south) t = roomTiles.Or(roomTiles.floorS, roomTiles.floorMid);

                floorTilemap.SetTile(new Vector3Int(x, y, 0), t);
            }
        }

        int rx0 = x0 - 1, rx1 = x1 + 1;
        int ry0 = y0 - 1, ry1 = y1 + 1;

        for (int x = rx0; x <= rx1; x++)
        {
            TileBase top;
            if (x == rx0) top = roomTiles.wallNW;
            else if (x == rx1) top = roomTiles.wallNE;
            else if (x == rx0 + 1) top = roomTiles.Or(roomTiles.wallNFirst, roomTiles.wallN);
            else top = roomTiles.wallN;

            TileBase bottom;
            if (x == rx0) bottom = roomTiles.wallSW;
            else if (x == rx1) bottom = roomTiles.wallSE;
            else bottom = roomTiles.wallS;

            SetWallIfWall(map, x, ry1, top);
            SetWallIfWall(map, x, ry0, bottom);
        }

        for (int y = ry0 + 1; y <= ry1 - 1; y++)
        {
            TileBase left = (y == ry0 + 1)
                ? roomTiles.Or(roomTiles.wallWBottom, roomTiles.wallW)
                : roomTiles.wallW;

            TileBase right = (y == ry0 + 1)
                ? roomTiles.Or(roomTiles.wallEBottom, roomTiles.wallE)
                : roomTiles.wallE;

            SetWallIfWall(map, rx0, y, left, useBackdrop: true);
            SetWallIfWall(map, rx1, y, right, useBackdrop: true);
        }
    }

    // Celija na kojoj je pod je vrata i preskace se, pa ulaz ostaje otvoren.
    // Podloga se slika u podnu tilemapu jer se ona crta ispod zidne.
    private void SetWallIfWall(TileType[,] map, int x, int y, TileBase tile,
                               bool useBackdrop = false)
    {
        if (tile == null) return;
        if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1)) return;
        if (map[x, y] != TileType.Wall) return;

        var pos = new Vector3Int(x, y, 0);

        if (useBackdrop && roomTiles.wallBackdrop != null)
            floorTilemap.SetTile(pos, roomTiles.wallBackdrop);

        wallTilemap.SetTile(pos, tile);
    }

    // Zidne plocice se crtaju samo uz rub poda (8-susjedstvo), cime broj
    // plocica i collidera pada za red velicine
    private static bool TouchesFloor(TileType[,] map, int x, int y)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (map[nx, ny] == TileType.Floor) return true;
            }
        }

        return false;
    }

    // Potpuno prozirna plocica; kolizija dolazi iz grid celije, ne iz sprite-a
    private static TileBase MakeInvisibleWallTile()
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
        texture.filterMode = FilterMode.Point;
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1),
                                   new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        return tile;
    }
}