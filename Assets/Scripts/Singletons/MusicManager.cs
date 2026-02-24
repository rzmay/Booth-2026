using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Metronome))]
[RequireComponent(typeof(SyncAudioSources))]
public class MusicManager : MonoBehaviour
{
    public enum MusicState
    {
        TutorialMenu,
        Gameplay,
        Victory,
        GameOver,
    }

    private static MusicManager _Instance;

    [System.Serializable]
    public class StateSong
    {
        public MusicState state;
        public AudioClip track;
    }

    [System.Serializable]
    public class GameSong
    {
        public string songName;

        // Always 4 clips -- bass, drums, vocals, inst
        [SerializeField] public AudioClip[] tracks = new AudioClip[4];
    }

    public MusicState startState = MusicState.TutorialMenu;

    [SerializeField] private List<StateSong> states;
    [SerializeField] private List<GameSong> levels;

    // How quickly does each track come in?
    public float volumePower = 0.25f;

    private MusicState _state;

    private SyncAudioSources _syncedAudio;
    private Metronome _metronome;

    public MusicState state
    {
        get { return _state; }
        set { SetState(value); }
    }

    void Awake()
    {
        _Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _syncedAudio = GetComponent<SyncAudioSources>();
        _metronome = GetComponent<Metronome>();

        SetState(startState);
    }

    void Update()
    {
        SyncStreak();
    }

    void SyncStreak()
    {
        List<float> progresses = new List<float>(StreakTracker.Instance.streakProgresses);
        List<float> volumes = progresses.Select(p => Mathf.Pow(p, volumePower)).ToList();
        SyncAudioSources.SetVolumes(volumes);
    }

    private void SetState(MusicState s, string songName = "")
    {
        _state = s;

        // Find level
        GameSong level = levels.Find(l => l.songName == songName);
        if (level == null) return;

        // Set the game music -- don't start yet
        List<float> volumes = new List<float>(new[] { 1.0f, 0f, 0f, 0f });
        SyncAudioSources.SetVolumes(volumes);
        SyncAudioSources.SetTracks(new List<AudioClip>(level.tracks));

        // Delegate starting the music to the metronome
        _metronome.Play();
    }
}
