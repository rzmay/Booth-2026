using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Scheduler))]
[RequireComponent(typeof(StreakTracker))]
public class TutorialScheduler : MonoBehaviour
{
    [System.Serializable]
    private class TutorialSegment
    {
        public Schedule schedule;
        public float requiredPoints;
    }

    [Header("Tutorial")]
    [SerializeField] private List<TutorialSegment> tutorialSegments = new();
    [SerializeField] private float segmentFinishBufferBeats = 0f;

    private Scheduler _scheduler;
    private StreakTracker _streakTracker;
    private Metronome _metronome;

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
        _currentSegmentIndex = 0;
        _tutorialRunning = true;
        _tutorialComplete = false;

        LoadCurrentSegment();
    }

    public void EndTutorial()
    {
        _tutorialRunning = false;
        _tutorialComplete = true;
    }

    private void LoadCurrentSegment()
    {
        if (tutorialSegments.Count == 0) return;
        if (_currentSegmentIndex < 0 || _currentSegmentIndex >= tutorialSegments.Count) return;

        TutorialSegment currentSegment = tutorialSegments[_currentSegmentIndex];
        if (currentSegment.schedule == null) return;

        _scheduler.LoadSchedule(currentSegment.schedule);
        _scheduler.Reset();
        _segmentStartScore = _streakTracker.score;
        _segmentFinishStartBeat = -1f;
    }

    private void CheckCurrentSegment()
    {
        if (!IsCurrentSegmentFinished()) return;

        float currentPoints = GetCurrentSegmentPoints();

        if (currentPoints >= tutorialSegments[_currentSegmentIndex].requiredPoints)
        {
            AdvanceToNextSegment();
        }
        else
        {
            RestartCurrentSegment();
        }
    }

    private void AdvanceToNextSegment()
    {
        _currentSegmentIndex++;

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

    private float GetCurrentSegmentPoints()
    {
        return _streakTracker.score - _segmentStartScore;
    }

    private bool IsCurrentSegmentFinished()
    {
        if (_scheduler.HasMoreEvents())
        {
            _segmentFinishStartBeat = -1f;
            return false;
        }

        float bufferBeats = segmentFinishBufferBeats;
        if (bufferBeats <= 0f || _metronome == null || !_metronome.isPlaying)
        {
            return true;
        }

        float currentBeat = _metronome.GetBeatFloat();

        if (_segmentFinishStartBeat < 0f)
        {
            _segmentFinishStartBeat = currentBeat;
            return false;
        }

        return currentBeat - _segmentFinishStartBeat >= clampedBufferBeats;
    }
}
