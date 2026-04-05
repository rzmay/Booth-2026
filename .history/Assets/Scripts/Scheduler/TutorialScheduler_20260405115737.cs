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

    private Scheduler _scheduler;
    private StreakTracker _streakTracker;

    private int _currentSegmentIndex;
    private float _segmentStartScore;
    private bool _tutorialRunning;
    private bool _tutorialComplete;

    void Awake()
    {
        _scheduler = GetComponent<Scheduler>();
        _streakTracker = GetComponent<StreakTracker>();
    }

    void Start()
    {
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
        // TODO: Move to the next segment.
        // TODO: End the tutorial if there are no more segments.
    }

    private void RestartCurrentSegment()
    {
        // TODO: Reset any per-attempt state if needed.
        // TODO: Restart the currently active segment.
    }

    private float GetCurrentSegmentPoints()
    {
        // TODO: Replace this with the final per-segment points calculation.
        return _streakTracker.score - _segmentStartScore;
    }

    private bool IsCurrentSegmentFinished()
    {
        // TODO: Replace this placeholder with the final segment completion check.
        return !_scheduler.HasMoreEvents();
    }
}
