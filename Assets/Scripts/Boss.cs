using System.Collections.Generic;
using UnityEngine;

// Boss u najvecoj sobi. Miruje dok igrac ne ude, tada se izlazi
// zabarikadiraju i pocinje borba: u pravilnim razmacima boss izvede napad
// i prizove lubanju. Nakon isteka vremena lubanje nestaju, boss odigra
// animaciju smrti i ostavi novcice.
// Jedna replika vezana uz sekundu borbe
[System.Serializable]
public class BossLine
{
    public float time;
    public string text;
}

[RequireComponent(typeof(SpriteRenderer))]
public class Boss : MonoBehaviour
{
    [Header("Animacije")]
    public Sprite[] idleFrames;
    public Sprite[] chargeFrames;
    public Sprite[] hitFrames;
    public float idleFps = 8f;
    public float chargeFps = 9f;
    public float hitFps = 6f;

    [Header("Tijek borbe")]
    public float fightDuration = 30f;
    public float spawnInterval = 3f;
    public float defeatAnimDuration = 6f;
    public int coinsOnDefeat = 10;

    [Tooltip("Odgoda prije zatvaranja izlaza, da igrac stigne uci u sobu")]
    public float barricadeDelay = 3f;

    [Header("Dijalog")]
    [Tooltip("Replika cim igrac ude u sobu, prije zatvaranja izlaza")]
    public string enterLine = "...";
    [Tooltip("Replike vezane uz sekundu borbe")]
    public BossLine[] fightLines =
    {
        new BossLine { time = 3f,  text = "You dare FACE ME!" },
        new BossLine { time = 10f, text = "You are fast.. GRR" },
        new BossLine { time = 20f, text = "I Fell... Weak..." },
    };
    public string deathLine = "I will get you next time.. NOOO..! AAARGH..!";

    [Header("Prefabi")]
    public GameObject skullPrefab;
    public GameObject barricadePrefab;

    private enum Phase { Waiting, Fighting, Defeated }

    private SpriteRenderer sr;
    private Phase phase = Phase.Waiting;

    private RectInt roomBounds;
    private List<Vector2Int> doorCells = new List<Vector2Int>();
    private GameObject coinPrefab;

    private readonly List<GameObject> skulls = new List<GameObject>();
    private readonly List<GameObject> barricades = new List<GameObject>();

    private Transform player;
    private float fightTimer;
    private float nextSpawnTime;
    private float chargeTimer;
    private float animTimer;
    private int animFrame;
    private bool barricadesPlaced;
    private int nextLineIndex;
    private BossDialog dialog;

    // Poziva generator odmah nakon stvaranja
    public void Setup(RectInt bounds, List<Vector2Int> doors, GameObject coins)
    {
        roomBounds = bounds;
        doorCells = doors ?? new List<Vector2Int>();
        coinPrefab = coins;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        dialog = GetComponent<BossDialog>();
    }

    private void Start()
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) player = pm.transform;
    }

    private void Update()
    {
        switch (phase)
        {
            case Phase.Waiting:
                Animate(idleFrames, idleFps);
                if (PlayerInRoom()) StartFight();
                break;

            case Phase.Fighting:
                UpdateFight();
                break;

            case Phase.Defeated:
                Animate(hitFrames, hitFps);
                fightTimer += Time.deltaTime;
                if (fightTimer >= defeatAnimDuration) Vanish();
                break;
        }
    }

    private bool PlayerInRoom()
    {
        if (player == null || !player.gameObject.activeInHierarchy) return false;

        var cell = new Vector2Int(Mathf.FloorToInt(player.position.x),
                                  Mathf.FloorToInt(player.position.y));
        return roomBounds.Contains(cell);
    }

    private void StartFight()
    {
        phase = Phase.Fighting;
        fightTimer = 0f;
        nextSpawnTime = spawnInterval;
        barricadesPlaced = false;
        nextLineIndex = 0;

        Say(enterLine);

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayBoss();
    }

    private void UpdateFight()
    {
        fightTimer += Time.deltaTime;

        if (!barricadesPlaced && fightTimer >= barricadeDelay)
            PlaceBarricades();

        // Replike vezane uz proteklo vrijeme borbe
        while (fightLines != null && nextLineIndex < fightLines.Length
               && fightTimer >= fightLines[nextLineIndex].time)
        {
            Say(fightLines[nextLineIndex].text);
            nextLineIndex++;
        }

        // Prizivanje u pravilnim razmacima; na samom kraju vise ne
        if (nextSpawnTime < fightDuration && fightTimer >= nextSpawnTime)
        {
            SpawnSkull();
            nextSpawnTime += spawnInterval;
        }

        if (chargeTimer > 0f)
        {
            chargeTimer -= Time.deltaTime;
            Animate(chargeFrames, chargeFps);
        }
        else
        {
            Animate(idleFrames, idleFps);
        }

        if (fightTimer >= fightDuration) EndFight();
    }

    // Izlazi se zatvaraju s odgodom, inace bi prepreka nikla tocno na
    // vratima kroz koja igrac ulazi
    private void PlaceBarricades()
    {
        barricadesPlaced = true;
        if (barricadePrefab == null) return;

        foreach (var cell in doorCells)
        {
            var b = Instantiate(barricadePrefab,
                                new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f),
                                Quaternion.identity, transform.parent);
            barricades.Add(b);
        }
    }

    private void SpawnSkull()
    {
        // Napad traje tocno jedan prolaz kroz svoje okvire
        if (chargeFrames != null && chargeFrames.Length > 0)
        {
            chargeTimer = chargeFrames.Length / Mathf.Max(0.01f, chargeFps);
            animFrame = 0;
            animTimer = 0f;
        }

        if (skullPrefab == null) return;

        // Pomak od sredista da lubanja ne nikne unutar bossovog collidera
        float angle = Random.Range(0f, Mathf.PI * 2f);
        var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.6f;

        skulls.Add(Instantiate(skullPrefab, transform.position + offset,
                               Quaternion.identity, transform.parent));
    }

    private void EndFight()
    {
        Say(deathLine);

        phase = Phase.Defeated;
        fightTimer = 0f;
        animFrame = 0;
        animTimer = 0f;

        foreach (var skull in skulls)
            if (skull != null) Destroy(skull);
        skulls.Clear();
    }

    private void Vanish()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMain(restart: true);

        foreach (var b in barricades)
            if (b != null) Destroy(b);
        barricades.Clear();

        if (coinPrefab != null)
        {
            for (int i = 0; i < coinsOnDefeat; i++)
            {
                int x = roomBounds.x + Random.Range(0, roomBounds.width);
                int y = roomBounds.y + Random.Range(0, roomBounds.height);
                Instantiate(coinPrefab, new Vector3(x + 0.5f, y + 0.5f, 0f),
                            Quaternion.identity, transform.parent);
            }
        }

        Destroy(gameObject);
    }

    private void Say(string line)
    {
        if (dialog != null && !string.IsNullOrEmpty(line))
            dialog.Show(line);
    }

    // Prazan okvir se preskace, da nepopunjeno polje ne sakrije bossa
    private void Animate(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;

        animTimer += Time.deltaTime;
        if (animTimer >= 1f / Mathf.Max(0.01f, fps))
        {
            animTimer = 0f;
            animFrame = (animFrame + 1) % frames.Length;
        }

        var frame = frames[Mathf.Clamp(animFrame, 0, frames.Length - 1)];
        if (frame != null) sr.sprite = frame;
    }
}