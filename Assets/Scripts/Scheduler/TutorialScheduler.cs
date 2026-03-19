using UnityEngine;

[RequireComponent(typeof(Scheduler))]
[RequireComponent(typeof(StreakTracker))]
[RequireComponent(typeof(Metronome))]
public class TutorialScheduler : MonoBehaviour
{
    [Header("Tutorial Source")]
    [SerializeField] private Schedule tutorialSequence;

    [Header("Progression")]
    [SerializeField] private float requiredPoints = 5f;
    [SerializeField] private bool autoplayOnStart = true;

    private Scheduler _scheduler;
    private StreakTracker _streakTracker;
    private bool _attemptRunning;
    private bool _tutorialStarted;
    private bool _tutorialComplete;
    private bool _waitingForAutostart;

    void Awake()
    {
        _scheduler = GetComponent<Scheduler>();
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
        if (_streakTracker == null) return;

        _tutorialComplete = false;
        _tutorialStarted = true;
        _waitingForAutostart = false;

        StartAttempt();
    }

    private void StartAttempt()
    {
        _scheduler.LoadSchedule(tutorialSequence);
        RewindTutorial();
        _streakTracker.streak = 0f;
        _attemptRunning = true;

        Debug.Log($"TutorialScheduler: started tutorial attempt, need {requiredPoints} total points to pass.");
    }

    private void FinishAttempt()
    {
        _attemptRunning = false;

        bool passed = _streakTracker.streak >= requiredPoints;

        if (!passed)
        {
            Debug.Log($"TutorialScheduler: failed tutorial with {_streakTracker.streak} / {requiredPoints} points, restarting.");
            _streakTracker.streak = 0f;
            StartAttempt();
            return;
        }

        Debug.Log($"TutorialScheduler: passed tutorial with {_streakTracker.streak} points.");
        _tutorialComplete = true;
        TutorialEnded();
    }

    private void RewindTutorial()
    {
        _scheduler.Reset();
    }
}
