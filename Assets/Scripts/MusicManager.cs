using UnityEngine;

// Glazba igre: stalna tema i tema za borbu s bossom.
// Sve ide kroz jedan AudioSource, pa se dvije pjesme nikad ne preklapaju.
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public AudioClip mainTheme;
    public AudioClip bossTheme;

    [Range(0f, 1f)]
    public float volume = 0.5f;

    private AudioSource source;

    private void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
    }

    private void Start()
    {
        PlayMain(restart: true);
    }

    // restart = false znaci "ako vec svira, pusti je da tece dalje"
    public void PlayMain(bool restart) => Play(mainTheme, restart);

    public void PlayBoss() => Play(bossTheme, restart: true);

    private void Play(AudioClip clip, bool restart)
    {
        if (clip == null) return;
        if (!restart && source.clip == clip && source.isPlaying) return;

        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.Play();
    }
}