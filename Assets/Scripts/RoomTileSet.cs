using UnityEngine;
using UnityEngine.Tilemaps;

// Plocice za prikaz sobe po 9-slice principu: zidni prsten oko sobe
// i pod sa sjenama uz rubove. Soba bilo koje velicine slika se istim skupom.
[System.Serializable]
public class RoomTileSet
{
    [Header("Zidni prsten - kutovi")]
    public TileBase wallNW;
    public TileBase wallNE;
    public TileBase wallSW;
    public TileBase wallSE;

    [Header("Zidni prsten - stranice")]
    public TileBase wallN;
    public TileBase wallS;
    public TileBase wallW;
    public TileBase wallE;

    [Header("Zidni prsten - varijante (neobavezno)")]
    public TileBase wallNFirst;
    public TileBase wallWBottom;
    public TileBase wallEBottom;

    [Header("Podloga ispod tankih bocnih plocica (neobavezno)")]
    public TileBase wallBackdrop;

    [Header("Pod sobe")]
    public TileBase floorMid;
    public TileBase floorNW;
    public TileBase floorW;
    public TileBase floorSW;
    public TileBase floorS;

    public bool IsUsable => floorMid != null && wallN != null;

    public TileBase Or(TileBase preferred, TileBase fallback)
        => preferred != null ? preferred : fallback;
}