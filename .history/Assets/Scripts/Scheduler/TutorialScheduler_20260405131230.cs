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
    private float _segmentStartBeat;
    private float _segmentDurationBeats;

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
        _segmentStartBeat = _metronome.GetBeatFloat();
        _segmentDurationBeats = GetScheduleDurationBeats(currentSegment.schedule);
        _segmentStartScore = _streakTracker.score;
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
            return false;
        }

        float currentBeat = _metronome.GetBeatFloat();
        return currentBeat - _segmentStartBeat >= _segmentDurationBeats + segmentFinishBufferBeats;
    }

    private float GetScheduleDurationBeats(Schedule schedule)
    {
        float lastBeat = 0f;

        foreach (Schedule.Event evt in schedule.events)
        {
            float eventBeat = evt.useTime ? _metronome.TimeToBeats(evt.time) : evt.beat - 1f;
            if (eventBeat > lastBeat)
            {
                lastBeat = eventBeat;
            }
        }

        return lastBeat;
    }
}
