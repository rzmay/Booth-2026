using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SyncAudioSources))]
public class MusicManager : MonoBehaviour
{
    public enum MusicState
    {
        MainMenu,
        Victory,
        GameOver,
        Gameplay,
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

    public MusicState startState = MusicState.MainMenu;

    [SerializeField] private List<StateSong> states;
    [SerializeField] private List<GameSong> levels;

    // We need four synced audio sources
    [SerializeField] private List<AudioSource> audioSources;

    private MusicState _state;

    private SyncAudioSources _syncedAudio;

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

        SetState(startState);
    }

    private void SetState(MusicState s, string songName = "")
    {
        _state = s;

        if (_state == MusicState.Gameplay)
        {
            // Find level
            GameSong level = levels.Find(l => l.songName == songName);
            if (level == null) return;

            // TODO: Delayable, add 8 beat metronome count-in

            // Start game music
            List<float> volumes = new List<float>(new[] { 1.0f, 0f, 0f, 0f });
            _syncedAudio.Play(new List<AudioClip>(level.tracks), volumes);
        }
        else
        {
            // Find state
            StateSong stateSong = states.Find(st => st.state == s);
            if (stateSong == null) return;

            // Play track
            _syncedAudio.PlayOne(stateSong.track);
        }
    }
}
