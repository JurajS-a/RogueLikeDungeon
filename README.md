# Thieves Crypt

2D top-down roguelike igra s proceduralnim generiranjem razina, izrađena u Unityju
kao završni rad na Fakultetu primijenjene matematike i informatike u Osijeku.

Razina se generira iznova pri svakom pokretanju i pri svakom silasku na sljedeću
razinu. Cilj je skupiti 20 novčića uz izbjegavanje neprijatelja i zamki.

## Kako radi generator

Generiranje ide u pet koraka:

1. **Razmještaj soba** nasumičnim postavljanjem s odbacivanjem (rejection sampling)
2. **Graf povezanosti** - potpun graf nad središtima soba, težine su udaljenosti
3. **Minimalno razapinjuće stablo** Primovim algoritmom, plus dio odbačenih bridova
   koji se vraća da nastanu alternativni putevi
4. **Izrezivanje hodnika** u L-obliku u matricu ćelija
5. **Prikaz** matrice u Unityjevom Tilemap sustavu

Prohodnost razine posljedica je svojstava razapinjućeg stabla, a ne naknadne
provjere. Izlaz se smješta u sobu koja je pretraživanjem u širinu utvrđena kao
najudaljenija od početne.

Sva slučajnost generiranja prolazi kroz jedan seed, pa ista vrijednost uvijek daje
istu razinu.

## Upravljanje

| Tipka | Radnja |
|---|---|
| WASD ili strelice | kretanje |
| ENTER | pokretanje igre |

## Pokretanje

Potreban je Unity 6 (6000.3 LTS) ili noviji.

1. Kloniraj repozitorij
2. Otvori mapu projekta u Unity Hubu
3. Otvori scenu `Assets/Scenes/SampleScene`
4. Pritisni Play

## Struktura koda

- `Assets/Scripts` - izvorni kod
- `Assets/Prefabs` - predlošci objekata koji nastaju tijekom generiranja
- `Assets/Tiles` - pločice za pod i zidove
- `Assets/Sprites` - slikovni materijal

Jezgru generatora čine `RoomPlacer`, `GraphBuilder`, `MstSolver`, `GraphSearch`,
`CorridorCarver`, `RoomDecorator` i `TilemapPainter`, a njima upravlja
`DungeonGenerator`.

## Prikaz rada algoritma

Na komponenti `DungeonGenerator` postoje četiri postavke za iscrtavanje
međurezultata u Scene prozoru: `Draw Map`, `Draw Rooms`, `Draw Graph` i
`Draw Candidate Edges`.

## Korišteni materijali

- **Ninja Adventure Asset Pack** — Pixel-Boy i AAA, licenca CC0,
  https://pixel-boy.itch.io/ninja-adventure-asset-pack
- **Dungeon Tileset II** — 0x72, licenca CC0,
  https://0x72.itch.io/dungeontileset-ii
