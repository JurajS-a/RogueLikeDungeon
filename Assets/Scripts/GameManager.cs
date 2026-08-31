using UnityEngine;
using UnityEngine.InputSystem;

// Stanje igre kao konacni automat: Menu -> Playing -> Dying -> GameOver,
// odnosno Won. Broji novcice i zivote te crta cijeli HUD i ekrane.
// Sucelje se crta immediate-mode pristupom (OnGUI), bez objekata u sceni.
public class GameManager : MonoBehaviour
{
    // Globalno dostupna instanca, da predmeti i neprijatelji mogu
    // javiti dogadaje bez rucno spojenih referenci
    public static GameManager Instance { get; private set; }

    public enum State { Menu, Playing, Dying, Won, GameOver }

    [Header("Reference")]
    public DungeonGenerator generator;
    public GameObject player;

    [Header("Pravila")]
    public int coinsToWin = 20;
    public int startingLives = 4;

    [Tooltip("Sekunde neranjivosti nakon udarca")]
    public float invulnerabilityTime = 1f;

    [Header("UI")]
    public Texture2D healthBarTexture;
    public int uiScale = 4;
    public Font uiFont;

    [Tooltip("Koliko razmaka zamjenjuje jedan razmak - font ima vrlo uzak razmak")]
    [Range(1, 6)]
    public int wordSpacing = 3;

    [Tooltip("Razmak medu redcima, kao visekratnik visine fonta")]
    public float lineSpacing = 2.2f;

    public Texture2D coinTexture;
    public float coinAnimFps = 8f;

    public Texture2D portraitTexture;
    public int portraitScale = 2;
    public int portraitBorder = 2;
    public Color portraitBorderColor = new Color(0.61f, 0.68f, 0.72f);

    [Header("Efekt udarca")]
    public CameraFollow cameraFollow;
    public float hitShakeDuration = 0.6f;
    public float hitShakeMagnitude = 0.3f;
    public float hitFlashDuration = 0.8f;
    [Range(0f, 1f)]
    public float hitFlashStrength = 0.35f;

    [Header("Smrt")]
    public float deathFadeDuration = 3f;

    private State state = State.Menu;
    private int coins;
    private int lives;
    private float lastHitTime = -999f;
    private float flashTimer;
    private float deathTimer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (player != null)
            player.SetActive(false);   // u meniju igrac ne postoji

        if (cameraFollow == null && Camera.main != null)
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }

    private void Update()
    {
        if (flashTimer > 0f)
            flashTimer -= Time.deltaTime;

        // Tijekom umiranja se ceka izbljedivanje i ignorira tipkovnica
        if (state == State.Dying)
        {
            deathTimer += Time.deltaTime;
            if (deathTimer >= deathFadeDuration)
            {
                state = State.GameOver;
                if (player != null) player.SetActive(false);
            }
            return;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        bool enterPressed = kb.enterKey.wasPressedThisFrame
                         || kb.numpadEnterKey.wasPressedThisFrame;

        if (state != State.Playing && enterPressed)
            StartGame();
    }

    // ---------- tijek igre ----------

    public void StartGame()
    {
        coins = 0;
        lives = startingLives;
        state = State.Playing;

        if (player != null)
        {
            player.SetActive(true);

            // ponisti posljedice prethodne smrti
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = true;

            var col = player.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            var animator = player.GetComponent<CharacterAnimator>();
            if (animator != null) animator.SetDead(false);
        }

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMain(restart: false);

        if (generator != null)
            generator.Generate();
    }

    public void AddCoin()
    {
        if (state != State.Playing) return;

        coins++;
        if (coins >= coinsToWin)
        {
            state = State.Won;
            if (player != null) player.SetActive(false);
        }
    }

    // Vraca false ako je zdravlje vec bilo puno, pa se napitak ne trosi
    public bool AddLife(int amount)
    {
        if (state != State.Playing) return false;
        if (lives >= startingLives) return false;

        lives = Mathf.Min(startingLives, lives + amount);
        return true;
    }

    public void PlayerHit()
    {
        if (state != State.Playing) return;

        // Neranjivost sprjecava da jedan kontakt skine vise zivota
        if (Time.time - lastHitTime < invulnerabilityTime) return;
        lastHitTime = Time.time;

        flashTimer = hitFlashDuration;
        if (cameraFollow != null)
            cameraFollow.Shake(hitShakeDuration, hitShakeMagnitude);

        lives--;
        if (lives <= 0) StartDying();
    }

    // Igrac ostaje u sceni kao les dok ekran ne izblijedi
    private void StartDying()
    {
        state = State.Dying;
        deathTimer = 0f;

        if (player == null) return;

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var col = player.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var animator = player.GetComponent<CharacterAnimator>();
        if (animator != null) animator.SetDead(true);
    }

    // ---------- sucelje ----------

    private void OnGUI()
    {
        switch (state)
        {
            case State.Menu:
                DrawCenteredScreen("THIEVES CRYPT", new[]
                {
                    "Move with WASD",
                    $"Collect {coinsToWin} coins",
                    "Press ENTER to start"
                });
                break;

            case State.Playing:
                DrawHitFlash();
                DrawHud();
                break;

            case State.Dying:
                DrawHud();
                DrawDeathFade();
                break;

            case State.Won:
                DrawCenteredScreen("YOU ESCAPED", new string[0]);
                break;

            case State.GameOver:
                DrawCenteredScreen("GAME OVER", new string[0]);
                break;
        }
    }

    // Velicine su visekratnici od 8 jer je font pixel-art
    private GUIStyle MakeStyle(int sizeUnits, TextAnchor anchor)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 8 * sizeUnits,
            alignment = anchor,
            wordWrap = false
        };

        if (uiFont != null) style.font = uiFont;
        else style.fontStyle = FontStyle.Bold;

        style.normal.textColor = Color.white;
        return style;
    }

    // Font ima vrlo uzak znak razmaka pa se rijeci slijepe;
    // umnazanjem razmaka dobiva se citljiv razmak bez diranja fonta
    private string Sp(string text)
        => text.Replace(" ", new string(' ', Mathf.Max(1, wordSpacing)));

    private void DrawHud()
    {
        const float margin = 20f;
        const float gap = 10f;

        float barX = margin;
        float barY = margin;

        if (portraitTexture != null)
        {
            int border = Mathf.Max(1, portraitBorder) * Mathf.Max(1, portraitScale);
            float size = portraitTexture.width * Mathf.Max(1, portraitScale);

            var prev = GUI.color;
            GUI.color = portraitBorderColor;
            GUI.DrawTexture(new Rect(margin, margin, size + border * 2, size + border * 2),
                            Texture2D.whiteTexture);
            GUI.color = prev;

            GUI.DrawTexture(new Rect(margin + border, margin + border, size, size),
                            portraitTexture);

            float barHeight = 16 * uiScale;
            barX = margin + size + border * 2 + gap;
            barY = margin + border + (size - barHeight) * 0.5f;
        }

        DrawHealthBar(barX, barY);
        DrawCoinCounter();
    }

    // Okvir bara ima cetiri prozirna slota, pa se bojani pravokutnici
    // crtaju ispod njega i vide se kroz proreze
    private void DrawHealthBar(float bx, float by)
    {
        if (healthBarTexture == null) return;

        int s = Mathf.Max(1, uiScale);
        const int slotX0 = 19, slotPitch = 15, slotW = 13, slotH = 7, slotY = 4;

        Color fill;
        if (lives >= 4)
            fill = new Color(0.42f, 0.75f, 0.19f);
        else if (lives >= 2)
            fill = new Color(0.87f, 0.44f, 0.15f);
        else
        {
            // zadnji zivot pulsira
            float pulse = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(Time.time * 7f));
            fill = new Color(0.67f, 0.20f, 0.20f, pulse);
        }

        var prevColor = GUI.color;
        GUI.color = fill;
        for (int i = 0; i < lives && i < 4; i++)
        {
            var slot = new Rect(bx + (slotX0 + i * slotPitch) * s,
                                by + slotY * s, slotW * s, slotH * s);
            GUI.DrawTexture(slot, Texture2D.whiteTexture);
        }
        GUI.color = prevColor;

        GUI.DrawTexture(new Rect(bx, by, 80 * s, 16 * s), healthBarTexture);
    }

    // Broj okvira novcica jednak je omjeru sirine i visine teksture
    private void DrawCoinCounter()
    {
        var style = MakeStyle(uiScale, TextAnchor.MiddleLeft);
        string text = $"{coins}/{coinsToWin}";
        Vector2 textSize = style.CalcSize(new GUIContent(text));

        const float margin = 20f;
        const float gap = 8f;
        float iconSize = (coinTexture != null ? coinTexture.height : 10) * uiScale;

        float totalWidth = (coinTexture != null ? iconSize + gap : 0f) + textSize.x;
        float x = Screen.width - margin - totalWidth;

        if (coinTexture != null)
        {
            int frameCount = Mathf.Max(1, coinTexture.width / coinTexture.height);
            int frame = Mathf.FloorToInt(Time.time * coinAnimFps) % frameCount;

            var texCoords = new Rect(frame / (float)frameCount, 0f, 1f / frameCount, 1f);
            GUI.DrawTextureWithTexCoords(
                new Rect(x, margin, iconSize, iconSize), coinTexture, texCoords);

            x += iconSize + gap;
        }

        GUI.Label(new Rect(x, margin, textSize.x, iconSize), text, style);
    }

    // Crveni preljev nakon udarca; crta se ispod HUD-a da brojaci ostanu citljivi
    private void DrawHitFlash()
    {
        if (flashTimer <= 0f || hitFlashDuration <= 0f) return;

        float t = Mathf.Clamp01(flashTimer / hitFlashDuration);

        var prev = GUI.color;
        GUI.color = new Color(0.75f, 0.08f, 0.08f, hitFlashStrength * t);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    // Zacrnjenje tijekom umiranja; crta se preko HUD-a
    private void DrawDeathFade()
    {
        if (deathFadeDuration <= 0f) return;

        float t = Mathf.Clamp01(deathTimer / deathFadeDuration);

        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, t);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    private void DrawCenteredScreen(string title, string[] lines)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var titleStyle = MakeStyle(uiScale * 3, TextAnchor.MiddleCenter);
        var textStyle = MakeStyle(uiScale, TextAnchor.MiddleCenter);

        float w = Screen.width;
        float h = Screen.height;

        GUI.Label(new Rect(0, h * 0.30f, w, h * 0.15f), Sp(title), titleStyle);

        float lineHeight = 8 * uiScale * lineSpacing;
        float y0 = h * 0.52f;
        for (int i = 0; i < lines.Length; i++)
            GUI.Label(new Rect(0, y0 + i * lineHeight, w, lineHeight),
                      Sp(lines[i]), textStyle);
    }
}