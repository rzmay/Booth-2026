using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Scheduler))]
[RequireComponent(typeof(StreakTracker))]
[RequireComponent(typeof(Metronome))]
public class TutorialScheduler : MonoBehaviour
{
    [Header("Tutorial Source")]
    [SerializeField] private Schedule tutorialSequence;

    [Header("Progression")]
    [SerializeField] private float requiredPoints = 2f;
    [SerializeField] private float checkpointAdvanceBeats = 1f;
    [SerializeField] private bool autoplayOnStart = true;

    private static readonly FieldInfo EventsField =
        typeof(Schedule).GetField("_events", BindingFlags.Instance | BindingFlags.NonPublic);

    private Scheduler _scheduler;
    private Metronome _metronome;
    private StreakTracker _streakTracker;

    private List<Schedule.Event> _lessons = new();
    private Schedule _runtimeLessonSchedule;
    private int _currentLessonIndex;
    private float _checkpointBeat;
    private float _attemptStartStreak;
    private bool _attemptRunning;
    private bool _tutorialStarted;
    private bool _tutorialComplete;
    private bool _waitingForAutostart;

    void Awake()
    {
        _scheduler = GetComponent<Scheduler>();
        _metronome = GetComponent<Metronome>();
        _streakTracker = GetComponent<StreakTracker>();
    }

    void Start()
    {
        _waitingForAutostart = autoplayOnStart;
    }

    void Update()
    {
        if (_waitingForAutostart)
        {
            TryBeginTutorial();
        }

        if (!_attemptRunning || _tutorialComplete) return;
        if (_scheduler.HasMoreEvents()) return;
        if (FindObjectsOfType<MovementCue>().Length > 0) return;

        FinishAttempt();
    }

    void OnDestroy()
    {
        if (_runtimeLessonSchedule != null)
        {
            Destroy(_runtimeLessonSchedule);
        }
    }

    public void BeginTutorial()
    {
        _waitingForAutostart = false;
        TryBeginTutorial(force: true);
    }

    public void EndTutorial()
    {
        _attemptRunning = false;
        _tutorialComplete = true;
        TutorialEnded();
    }

    public void TutorialEnded()
    {
    }

    private void TryBeginTutorial(bool force = false)
    {
        if (_tutorialStarted && !force) return;

        if (tutorialSequence == null)
        {
            tutorialSequence = _scheduler.schedule;
        }

        if (tutorialSequence == null) return;
        if (_metronome == null || _streakTracker == null) return;

        BuildLessonList();

        if (_lessons.Count == 0)
        {
            Debug.LogWarning("TutorialScheduler could not find any top-level tutorial events.");
            _waitingForAutostart = false;
            enabled = false;
            return;
        }

        _currentLessonIndex = 0;
        _checkpointBeat = 0f;
        _tutorialComplete = false;
        _tutorialStarted = true;
        _waitingForAutostart = false;

        StartAttempt();
    }

    private void BuildLessonList()
    {
        if (EventsField == null)
        {
            Debug.LogWarning("TutorialScheduler could not read Schedule._events.");
            _lessons = new List<Schedule.Event>();
            return;
        }

        List<Schedule.Event> rawEvents = EventsField.GetValue(tutorialSequence) as List<Schedule.Event>;
        if (rawEvents == null)
        {
            _lessons = new List<Schedule.Event>();
            return;
        }

        float bpm = _metronome != null && _metronome.bpm > 0 ? _metronome.bpm : 120f;
        _lessons = rawEvents
            .Select(CloneEvent)
            .OrderBy(e => e.GetCanonTime(bpm))
            .ToList();
    }

    private void StartAttempt()
    {
        if (_runtimeLessonSchedule != null)
        {
            Destroy(_runtimeLessonSchedule);
        }

        _runtimeLessonSchedule = ScriptableObject.CreateInstance<Schedule>();
        List<Schedule.Event> lessonEvents = new() { CloneEvent(_lessons[_currentLessonIndex]) };
        EventsField?.SetValue(_runtimeLessonSchedule, lessonEvents);

        _scheduler.LoadSchedule(_runtimeLessonSchedule);
        RewindSchedulerToCheckpoint(_checkpointBeat);

        _attemptStartStreak = _streakTracker.streak;
        _attemptRunning = true;
    }

    private void FinishAttempt()
    {
        _attemptRunning = false;

        float scoreDelta = _streakTracker.streak - _attemptStartStreak;
        bool passed = scoreDelta >= requiredPoints;

        if (!passed)
        {
            StartAttempt();
            return;
        }

        _checkpointBeat = GetCheckpointAfter(_lessons[_currentLessonIndex]);
        _currentLessonIndex++;

        if (_currentLessonIndex >= _lessons.Count)
        {
            _tutorialComplete = true;
            TutorialEnded();
            return;
        }

        StartAttempt();
    }

    private void RewindSchedulerToCheckpoint(float checkpointBeat)
    {
        _scheduler.Reset();

        // Reset() rewinds relative beat/time to 0. Shift that baseline forward to the checkpoint.
        _scheduler.beatOffset -= checkpointBeat;
        _scheduler.songTimeOffset -= _metronome.BeatsToTime(checkpointBeat);
    }

    private float GetCheckpointAfter(Schedule.Event lesson)
    {
        return GetLessonBeat(lesson) + checkpointAdvanceBeats;
    }

    private float GetLessonBeat(Schedule.Event lesson)
    {
        if (lesson.time >= 0f)
        {
            return _metronome.TimeToBeats(lesson.time) + 1f;
        }

        return lesson.beat;
    }

    private static Schedule.Event CloneEvent(Schedule.Event source)
    {
        return new Schedule.Event
        {
            beat = source.beat,
            time = source.time,
            item = source.item,
            subschedule = source.subschedule,
            position = source.position,
            scale = source.scale,
            rotation = source.rotation,
        };
    }
}
