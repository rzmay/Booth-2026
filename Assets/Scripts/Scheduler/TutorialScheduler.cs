using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Scheduler))]
[RequireComponent(typeof(StreakTracker))]
public class TutorialScheduler : MonoBehaviour
{

    [Header("Tutorial")]
    [SerializeField] private List<TutorialSegment> tutorialSegments = new();

    [Header("UI")]
    [SerializeField] private TMP_Text _text;

    private Scheduler _scheduler;
    private StreakTracker _streakTracker;
    private Metronome _metronome;
    private CalibrationManager _calibrationManager;

    private int _currentSegmentIndex;
    private float _segmentStartScore;
    private bool _tutorialRunning;
    private bool _tutorialComplete;
    private float _segmentFinishStartBeat = -1f;

    void Awake()
    {
        _scheduler = GetComponent<Scheduler>();
        _streakTracker = GetComponent<StreakTracker>();
        _metronome = GetComponent<Metronome>();
        _calibrationManager = CalibrationManager.Instance;
        BindTutorialSegmentBoolSources();
    }

    [System.Serializable]
    private class TutorialSegment
    {
        public Schedule schedule;
        [TextArea] public string text;
        [SerializeField] public List<GameObject> gameObjects;
        public float requiredPoints;
        public bool useBool;
        public BoolValueSource boolValue;
        public float bufferBeats;
    }

    void Start()
    {
        _currentSegmentIndex = 0;
        _tutorialRunning = false;
        _tutorialComplete = false;
    }

    void Update()
    {
        if (!_tutorialRunning || _tutorialComplete) return;

        CheckCurrentSegment();
    }

    public void BeginTutorial()
    {
        if (_calibrationManager == null)
        {
            _calibrationManager = CalibrationManager.Instance;
        }
        BindTutorialSegmentBoolSources();

        //set calibration to false
        _calibrationManager.SetCalibrationBool(false);
        _currentSegmentIndex = 0;
        _tutorialRunning = true;
        _tutorialComplete = false;

        LoadCurrentSegment();
    }

    public void EndTutorial()
    {
        _tutorialRunning = false;
        _tutorialComplete = true;

        // Open the level select
        MenuController.SetMenu(1);
    }

    public void TryAdvanceCurrentSegment()
    {
        if (!_tutorialRunning || _tutorialComplete) return;

        CheckCurrentSegment();
    }

    private void LoadCurrentSegment()
    {
        if (tutorialSegments.Count == 0) return;
        if (_currentSegmentIndex < 0 || _currentSegmentIndex >= tutorialSegments.Count) return;

        TutorialSegment currentSegment = tutorialSegments[_currentSegmentIndex];
        if (currentSegment.schedule == null) return;

        _scheduler.LoadSchedule(currentSegment.schedule);
        _scheduler.Reset();

        // Reset streak rather than tracking the new score
        _streakTracker.streak = _segmentStartScore;
        _segmentFinishStartBeat = -1f;

        _text.text = currentSegment.text;

        SetGameObjectsActive(_currentSegmentIndex - 1, false);
        SetGameObjectsActive(_currentSegmentIndex, true);
    }

    private void SetGameObjectsActive(int i, bool active)
    {
        if (i < 0 || tutorialSegments.Count <= i) return;

        foreach (GameObject go in tutorialSegments[i].gameObjects)
        {
            go.SetActive(active);
        }
    }

    private void CheckCurrentSegment()
    {
        if (!IsCurrentSegmentFinished()) return;

        TutorialSegment currentSegment = tutorialSegments[_currentSegmentIndex];
        if (currentSegment.useBool && !currentSegment.boolValue.Value) return;

        float currentPoints = GetCurrentSegmentPoints();

        if (currentPoints >= currentSegment.requiredPoints)
        {
            AdvanceToNextSegment();
        }
        else
        {
            RestartCurrentSegment();
        }
    }

    public void AdvanceToNextSegment()
    {
        _currentSegmentIndex++;

        // Track starting score
        _segmentStartScore = _streakTracker.streak;

        if (_currentSegmentIndex >= tutorialSegments.Count)
        {
            EndTutorial();
        }
        else
        {
            LoadCurrentSegment();
        }
    }

    private void RestartCurrentSegment()
    {
        LoadCurrentSegment();
    }

    private void BindTutorialSegmentBoolSources()
    {
        if (_calibrationManager == null) return;

        foreach (TutorialSegment tutorialSegment in tutorialSegments)
        {
            if (tutorialSegment == null) continue;
            tutorialSegment.boolValue = _calibrationManager;
        }
    }

    private float GetCurrentSegmentPoints()
    {
        Debug.Log($"_streakTracker.streak[{_streakTracker.streak}] - _segmentStartScore[{_segmentStartScore}] = {_streakTracker.streak - _segmentStartScore}");
        return _streakTracker.streak - _segmentStartScore;
    }

    private bool IsCurrentSegmentFinished()
    {
        if (_scheduler.HasMoreEvents())
        {
            _segmentFinishStartBeat = -1f;
            return false;
        }

        // update bufferBeats
        float bufferBeats = tutorialSegments[_currentSegmentIndex].bufferBeats;

        float currentBeat = _metronome.GetBeatFloat();

        // update the segment finish start beat if it hasn't been set yet
        if (_segmentFinishStartBeat < 0f)
        {
            _segmentFinishStartBeat = currentBeat;
            return false;
        }

        return currentBeat - _segmentFinishStartBeat >= bufferBeats;
    }
}
