using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Scheduler))]
[RequireComponent(typeof(Metronome))]
[RequireComponent(typeof(StemMixer))]
public class MusicManager : MonoBehaviour
{
    public static StemMixer Stems { get { return _Instance._stems; } }
    public static Metronome Metronome { get { return _Instance._metronome; } }
    private static MusicManager _Instance;
    public enum MusicState
    {
        TutorialMenu,
        Gameplay,
        GameOver,
    }

    [System.Serializable]
    public class StateConfig
    {
        public MusicState state;
        public SongData songData;
    }

    public MusicState startState = MusicState.TutorialMenu;

    [SerializeField] private List<StateConfig> states;
    [SerializeField] private InputActionReference tutorialCalibrationAction;

    // How quickly does each track come in?
    public float volumePower = 0.25f;

    private MusicState _state;

    private StemMixer _stems;
    private Metronome _metronome;
    private StreakTracker _streakTracker;
    private Scheduler _scheduler;
    private CalibrationManager _calibrationManager;
    private TutorialScheduler _tutorialScheduler;
    private RawImage _screenEffectImage;
    private bool _isTutorialScene => startState == MusicState.TutorialMenu;

    public MusicState state
    {
        get { return _state; }
        set { SetState(value); }
    }

    void Awake()
    {
        _Instance = this;

        _stems = GetComponent<StemMixer>();
        _metronome = GetComponent<Metronome>();
        _streakTracker = GetComponent<StreakTracker>();
        _scheduler = GetComponent<Scheduler>();
        _calibrationManager = _calibrationManager.Instance;
        _tutorialScheduler = GetComponent<TutorialScheduler>();

        // There should only be one
        _screenEffectImage = Object.FindFirstObjectByType<ScreenEffect>().GetComponent<RawImage>();
    }

    void OnEnable()
    {
        if (_isTutorialScene)
        {
            tutorialCalibrationAction.action.performed += OnTutorialCalibrationAction;
        }
    }

    void OnDisable()
    {
        if (_isTutorialScene)
        {
            tutorialCalibrationAction.action.performed -= OnTutorialCalibrationAction;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetState(startState);

        if (startState == MusicState.TutorialMenu)
        {
            if (_tutorialScheduler != null)
            {
                _tutorialScheduler.BeginTutorial();
            }
        }
    }

    void Update()
    {
        SyncStreak();
    }

    void SyncStreak()
    {
        List<float> progresses = new List<float>(_streakTracker.streakProgresses);
        List<float> volumes = progresses.Select(p => Mathf.Pow(p, volumePower)).ToList();

        // Base track should always be full volume
        volumes[0] = 1.0f;

        _stems.SetVolumes(volumes);
    }

    private void SetState(MusicState s, string songName = null)
    {
        _state = s;

        // Find matching state and song name if applicable
        StateConfig config = states.Find(s => s.state == _state && (songName == null ? true : s.songData.songName == songName));
        if (config == null) return;

        // Load song data into metronome
        _metronome.LoadSongData(config.songData); // Metronome needs to load first for schedule sorting
        _streakTracker.LoadSongData(config.songData);
        _scheduler.LoadSchedule(config.songData.schedule);

        // Load screen effect
        _screenEffectImage.material = config.songData.screenEffectMaterial;

        // Set the game music -- don't start yet
        List<float> volumes = new List<float>(new[] { 1.0f, 0f, 0f, 0f });
        _stems.SetVolumes(volumes, true);
        _stems.SetTracks(new List<AudioClip>(config.songData.tracks));

        // Set loop time
        double loopTime = _metronome.BeatsToTime(config.songData.loopToBeats);
        _stems.SetLoopTime(loopTime);

        // Reset schedule
        _scheduler.Reset();

        // Delegate starting the music to the metronome
        _metronome.Play();
    }

    private void OnTutorialCalibrationAction(InputAction.CallbackContext obj)
    {
        if (!_isTutorialScene || _calibrationManager.CurrentCalibration.isCalibrated)
        {
            return;
        }

        _calibrationManager.doCalibrationExternal();
        _tutorialScheduler.AdvanceToNextSegment();
    }
}
