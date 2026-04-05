using UnityEngine;

/// <summary>
/// Very basic skeleton for a tutorial-specific scheduler.
/// Intentionally contains signatures + comments only.
/// </summary>
[RequireComponent(typeof(Scheduler))]
[RequireComponent(typeof(Metronome))]
public class TutorialScheduler : MonoBehaviour
{
    public enum TutorialStep
    {
        None,
        SingleHit,
        SinglePerfect,
        ThreeBeats,
        ThreeBeatsBothHands,
        Complete
    }

    [Header("References")]
    [SerializeField] private Scheduler _scheduler;
    [SerializeField] private Metronome _metronome;

    [Header("Runtime")]
    [SerializeField] private TutorialStep _currentStep = TutorialStep.None;

    void Awake()
    {
        if (_scheduler == null)
        {
            _scheduler = GetComponent<Scheduler>();
        }

        if (_metronome == null)
        {
            _metronome = GetComponent<Metronome>();
        }

        if (_scheduler == null || _metronome == null)
        {
            Debug.LogError("TutorialScheduler requires Scheduler and Metronome components.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (_metronome == null)
        {
            return;
        }

        _metronome.OnBeat += HandleBeat;
        _metronome.OnMetronomeTime += HandleMetronomeTime;
    }

    void OnDisable()
    {
        // TODO:
        // - Unsubscribe from events registered in OnEnable.
        _metronome.OnBeat -= HandleBeat;
        _metronome.OnMetronomeTime -= HandleMetronomeTime;

    }

    public void StartTutorial()
    {
        // TODO:
        // - Reset tutorial progress.
        // - Enter first tutorial step.
        // - Prime scheduler/metronome state for tutorial flow.


    }

    public void StopTutorial()
    {
        // TODO:
        // - Stop tutorial progression.
        // - Clear active tutorial cues if needed.


    }

    public void SkipToStep(TutorialStep step)
    {
        // TODO:
        // - Debug/helper method to jump to a specific step.
        // - Should call EnterStep(step).


    }

    private void EnterStep(TutorialStep step)
    {
        // TODO:
        // - Set _currentStep.
        // - Build/apply the schedule for this step.
        // - Reset per-step counters and pass/fail requirements.


    }

    private void ExitStep(TutorialStep step)
    {
        // TODO:
        // - Cleanup any step-local state before changing steps.


    }

    private void AdvanceStep()
    {
        // TODO:
        // - Move from current step to next one.
        // - Mark Complete when all steps are done.


    }

    private void RetryStep()
    {
        // TODO:
        // - Re-enter current step after fail conditions.
        // - Rebuild or replay current step schedule.


    }

    private Schedule BuildScheduleForStep(TutorialStep step)
    {
        // TODO:
        // - Return a small, hard-wired Schedule for the requested step:
        //   1) Single hit
        //   2) Single perfect
        //   3) Three beats in a row
        //   4) Three beats with both hands at once


        return null;
    }

    private void ApplyStepSchedule(Schedule stepSchedule)
    {
        // TODO:
        // - Assign stepSchedule to _scheduler.schedule.
        // - Reset _scheduler offsets/index so this step starts fresh.


    }

    private void HandleCueResult(/* MovementCue cue, MovementCue.Result result */)
    {
        // TODO:
        // - Evaluate incoming cue result against current step rule.
        // - Update per-step progress counters.
        // - Call AdvanceStep() or RetryStep() when appropriate.


    }

    private void HandleBeat(int beatIndex, double beatDspTime)
    {
        if (_currentStep == TutorialStep.None || _currentStep == TutorialStep.Complete)
        {
            return;
        }

        if (IsStepPassed())
        {
            AdvanceStep();
        }
    }

    private void HandleMetronomeTime(float beatFloat, double songTime)
    {
        if (_currentStep == TutorialStep.None || _currentStep == TutorialStep.Complete)
        {
            return;
        }

        if (IsStepFailed())
        {
            RetryStep();
        }
    }

    private bool IsStepPassed()
    {
        // TODO:
        // - Return true when current step requirements are satisfied.


        return false;
    }

    private bool IsStepFailed()
    {
        // TODO:
        // - Return true when current step should be retried.


        return false;
    }
}
