using UnityEngine;

// Dijaloska kutija na dnu ekrana tijekom borbe s bossom.
// Crta se immediate-mode pristupom, kao i ostatak sucelja.
// Mjere okna za ikonicu i tekstualnog podrucja odgovaraju slici kutije.
public class BossDialog : MonoBehaviour
{
    [Header("Slike")]
    public Texture2D boxTexture;
    public Texture2D portraitTexture;

    [Header("Sadrzaj")]
    public string speakerName = "Bamboss";
    public float lineDuration = 5f;

    [Header("Prikaz")]
    [Tooltip("Povecanje kutije; automatski se smanjuje ako ne stane na ekran")]
    public int boxScale = 4;
    public float bottomMargin = 24f;
    public int nameFontUnits = 4;
    public int textFontUnits = 5;

    [Tooltip("Odmak teksta od lijevog ruba tekstualnog podrucja, u izvornim pikselima")]
    public int textPadding = 8;
    public Color nameColor = new Color(0.59f, 0.33f, 0.25f);
    public Color textColor = new Color(0.12f, 0.14f, 0.14f);

    // izmjereno iz slike kutije (300x58)
    private const int SlotX = 6, SlotY = 14, SlotSize = 38;
    private const int TextX = 50, TextW = 244;

    private string currentLine;
    private float timer;

    public void Show(string line)
    {
        currentLine = line;
        timer = lineDuration;
    }

    private void Update()
    {
        if (timer > 0f) timer -= Time.deltaTime;
    }

    private void OnGUI()
    {
        if (timer <= 0f || boxTexture == null || string.IsNullOrEmpty(currentLine)) return;

        // Kutija se smanjuje ako je ekran preuzak
        int maxScale = Mathf.Max(1, Mathf.FloorToInt((Screen.width - 40f) / boxTexture.width));
        int s = Mathf.Clamp(boxScale, 1, maxScale);

        float boxW = boxTexture.width * s;
        float boxH = boxTexture.height * s;
        float x = (Screen.width - boxW) * 0.5f;
        float y = Screen.height - boxH - bottomMargin;

        GUI.DrawTexture(new Rect(x, y, boxW, boxH), boxTexture);

        if (portraitTexture != null)
            GUI.DrawTexture(new Rect(x + SlotX * s, y + SlotY * s,
                                     SlotSize * s, SlotSize * s), portraitTexture);

        float textX = x + (TextX + textPadding) * s;
        float textY = y + SlotY * s;
        float textW = (TextW - textPadding * 2) * s;

        float nameHeight = 8 * nameFontUnits * 1.4f;

        GUI.Label(new Rect(textX, textY, textW, nameHeight),
                  Spaced(speakerName), MakeStyle(nameFontUnits, nameColor));

        GUI.Label(new Rect(textX, textY + nameHeight, textW, 8 * textFontUnits * 1.6f),
                  Spaced(currentLine), MakeStyle(textFontUnits, textColor));
    }

    private GUIStyle MakeStyle(int sizeUnits, Color color)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 8 * sizeUnits,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false
        };

        // Font se preuzima iz GameManagera, da sucelje bude jedinstveno
        if (GameManager.Instance != null && GameManager.Instance.uiFont != null)
            style.font = GameManager.Instance.uiFont;
        else
            style.fontStyle = FontStyle.Bold;

        style.normal.textColor = color;
        return style;
    }

    // Isti razlog kao u GameManageru: font ima vrlo uzak znak razmaka
    private string Spaced(string text)
    {
        int n = GameManager.Instance != null ? GameManager.Instance.wordSpacing : 3;
        return text.Replace(" ", new string(' ', Mathf.Max(1, n)));
    }
}