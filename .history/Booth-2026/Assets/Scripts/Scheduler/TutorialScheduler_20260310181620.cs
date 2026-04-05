using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Very basic skeleton for a tutorial-specific scheduler.
/// Intentionally contains signatures + comments only.
/// </summary>
[RequireComponent(typeof(Scheduler))]
[RequireComponent(typeof(Metronome))]
public class TutorialScheduler : MonoBehaviour
{
    [System.Serializable]
    private class TutorialSubSchedule
    {
        public TutorialStep step = TutorialStep.None;
        public Schedule schedule;
        public int requiredProgress = 0;
        public int maxFailures = -1; // -1 means unlimited failures
        public bool requirePerfectResults = false;
    }

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
    [SerializeField] private List<TutorialSubSchedule> _subSchedules = new();

    [Header("Runtime")]
    [SerializeField] private TutorialStep _currentStep = TutorialStep.None;
    [SerializeField] private int _activeSubScheduleIndex = -1;
    [SerializeField] private int _stepProgress;
    [SerializeField] private int _stepFailures;
    [SerializeField] private int _requiredProgress;
    [SerializeField] private int _maxFailures;
    [SerializeField] private bool _requirePerfectResults;

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
        if (_metronome == null)
        {
            return;
        }
        _metronome.OnBeat -= HandleBeat;
        _metronome.OnMetronomeTime -= HandleMetronomeTime;
    }

    public void StartTutorial()
    {
        if (_scheduler == null || _metronome == null || _subSchedules.Count == 0)
        {
            return;
        }

        _currentStep = TutorialStep.None;
        _scheduler.Reset();

        if (!_metronome.isPlaying)
        {
            _metronome.Play();
        }

        EnterSubSchedule(0);
    }

    public void StopTutorial()
    {
        if (_scheduler == null || _metronome == null)
        {
            return;
        }

        _currentStep = TutorialStep.None;
        _scheduler.Reset();

        if (_metronome.isPlaying)
        {
            _metronome.Stop();
        }
    }

    public void SkipToStep(TutorialStep step)
    {
        EnterStep(step);
    }

    private void EnterStep(TutorialStep step)
    {
        int stepIndex = GetSubScheduleIndex(step);
        if (stepIndex < 0)
        {
            return;
        }

        EnterSubSchedule(stepIndex);
    }

    private void EnterSubSchedule(int stepIndex)
    {
        if ((uint)stepIndex >= (uint)_subSchedules.Count)
        {
            return;
        }

        TutorialSubSchedule subSchedule = _subSchedules[stepIndex];
        _activeSubScheduleIndex = stepIndex;
        _currentStep = subSchedule.step;

        ApplyStepSchedule(subSchedule.schedule);

        _stepProgress = 0;
        _stepFailures = 0;
        _requiredProgress = Mathf.Max(0, subSchedule.requiredProgress);
        _maxFailures = subSchedule.maxFailures;
        _requirePerfectResults = subSchedule.requirePerfectResults;
    }

    private void ExitStep(TutorialStep step)
    {
        // TODO:
        // - Cleanup any step-local state before changing steps.


    }

    private void AdvanceStep()
    {
        int nextIndex = _activeSubScheduleIndex + 1;
        if ((uint)nextIndex >= (uint)_subSchedules.Count)
        {
            _activeSubScheduleIndex = -1;
            _currentStep = TutorialStep.Complete;
            return;
        }

        EnterSubSchedule(nextIndex);
    }

    private void RetryStep()
    {
        if ((uint)_activeSubScheduleIndex >= (uint)_subSchedules.Count)
        {
            return;
        }

        EnterSubSchedule(_activeSubScheduleIndex);
    }

    private Schedule BuildScheduleForStep(TutorialStep step)
    {
        int stepIndex = GetSubScheduleIndex(step);
        if (stepIndex < 0)
        {
            return null;
        }

        return _subSchedules[stepIndex].schedule;
    }

    private void ApplyStepSchedule(Schedule stepSchedule)
    {
        _scheduler.schedule = stepSchedule;
        _scheduler.Reset();
    }

    public void HandleCueResult(MovementCue.Result result)
    {
        if (_currentStep == TutorialStep.None || _currentStep == TutorialStep.Complete)
        {
            return;
        }

        bool isSuccess = _requirePerfectResults ? result == MovementCue.Result.Perfect :
            result == MovementCue.Result.OnTime || result == MovementCue.Result.Perfect;

        if (isSuccess)
        {
            _stepProgress++;
        }
        else
        {
            _stepFailures++;
        }
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
        bool progressSatisfied = _stepProgress >= _requiredProgress;
        bool scheduleFinished = !_scheduler.HasMoreEvents();
        return progressSatisfied && scheduleFinished;
    }

    private bool IsStepFailed()
    {
        if (_maxFailures < 0)
        {
            return false;
        }

        return _stepFailures > _maxFailures;
    }

    private int GetSubScheduleIndex(TutorialStep step)
    {
        for (int i = 0; i < _subSchedules.Count; i++)
        {
            if (_subSchedules[i].step == step)
            {
                return i;
            }
        }

        return -1;
    }
}
