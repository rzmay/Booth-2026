using System;
using System.Collections.Generic;
using UnityEngine;

// SCHEDULER CLASS
// handles the scheduling of events based on the metronome's beat timing, allowing for precise timing of gameplay events and visual effects in sync with the music
[RequireComponent(typeof(Metronome))]
public sealed class Scheduler : MonoBehaviour
{
    private enum TutorialState
    {
        Disabled,
        PlayingMasterUntilGate,
        PlayingRepeat,
        WaitingToReplay,
        PlayingTail,
        Complete,
    }

    private sealed class TutorialSegment
    {
        public List<Schedule.Event> events = new();
        public double leadIn = 0d;
    }

    private sealed class TutorialCheckpoint
    {
        public Schedule schedule;
        public float requiredPoints;
        public double gateStartTime;
        public double repeatStartTime;
        public double repeatEndTime;
        public double gateTrackingStartTime;
        public List<Schedule.Event> gateEvents = new();
        public List<Schedule.Event> repeatEvents = new();

        public string Name
        {
            get { return schedule?.name ?? "<none>"; }
        }
    }

    private sealed class TutorialScheduleOccurrence
    {
        public Schedule schedule;
        public double startTime;
        public double endTime;
    }

    // SCHEDULER WORKFLOW:
    // 1. scheduler subscribes to the metronome events (VisualizerOnBeat and ScoringEvent) to receive beat timing information
    // 2. loads ScriptableObject schedules that has a list of events to be scheduled, each with specific timing and parameters
    // 3. keeps a pointer nextIndex into the list of events to always know the next event to be scheduled
    // 4. runs update loop. If an event is due to run on timestep curTime + scheduleAheadTime, then we instantiate the prefab and trigger the event, and move the pointer to the next event. This allows us to schedule events slightly ahead of time to ensure they are triggered precisely on beat.

    // VARIABLE INITIALIZATION //
    [SerializeField] public Schedule schedule;

    [Header("Tutorial")]
    [SerializeField] private bool tutorialMode = false;
    [SerializeField] private List<Schedule> tutorialRepeatSchedules = new();
    [SerializeField] private List<float> tutorialRequiredPointsBySchedule = new();
    [SerializeField] private float tutorialFallbackRequiredPoints = 5f;
    [SerializeField] private float tutorialReplayDelayBeats = 1f;

    private Metronome _metronome;
    private StreakTracker _streakTracker;

    private int _nextIndex = 0;

    private double _songTime;
    private float _beat;

    /* Used for resetting.
        * Although songTimeOffset should be synced to beats in this code,
        * the variables remain public and will be allowed to be separate
        * so they can be influenced more flexibly by other scripts.
    */
    public float beatOffset = 0;
    public double songTimeOffset = 0;
    // Whether or not to spawn items relative to the location of the player. This can be changed to "false" later on if we want a more robust calibration
    public bool spawnRelative = true;
    public float getTransform = -2f; // At what beat do we grab the headset transform for the origin?
    public float heightOffset = -0.5f;
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private bool _gotTransform = false;

    private List<Schedule.Event> _events = new();
    private List<Schedule.Event> _tutorialGateEvents = new();
    private List<Schedule.Event> _tutorialRepeatEvents = new();
    private List<Schedule.Event> _tutorialTailEvents = new();
    private List<Schedule.Event> _tutorialMasterEvents = new();
    private List<TutorialCheckpoint> _tutorialCheckpoints = new();
    private TutorialState _tutorialState = TutorialState.Disabled;
    private int _tutorialCheckpointIndex = -1;
    private float _tutorialReplayBeat = -1f;
    private double _tutorialRepeatStartTime = 0d;
    private double _tutorialRepeatEndTime = 0d;
    private double _tutorialTrackingStartTime = 0d;
    private bool _tutorialTrackingPoints = false;
    private int _tutorialAttemptNumber = 0;
    private float _tutorialCurrentRequiredPoints = 0f;
    private string _tutorialCurrentScheduleName = "<none>";

    void Awake()
    {
        _metronome = GetComponent<Metronome>();
        _streakTracker = GetComponent<StreakTracker>();
    }

    public void Start()
    {
        _nextIndex = 0;
    }

    // subscribe to metronome events
    void OnEnable()
    {
        _metronome.OnMetronomeTime += OnMetronomeTime;
    }

    // unsubscribe to metronome events
    void OnDisable()
    {
        _metronome.OnMetronomeTime -= OnMetronomeTime;
    }

    void Update()
    {
        if (!tutorialMode) return;

        ActivateTutorialPointTrackingIfNeeded();

        switch (_tutorialState)
        {
            case TutorialState.PlayingMasterUntilGate:
            case TutorialState.PlayingRepeat:
                if (HasMoreEvents()) return;
                if (HasActiveMovementCues()) return;

                FinishTutorialAttempt();
                return;
            case TutorialState.WaitingToReplay:
                if (_tutorialReplayBeat >= 0f && _beat >= _tutorialReplayBeat)
                {
                    StartTutorialRepeatAttempt();
                }

                return;
            default:
                return;
        }
    }

    // subscribed to OnMetronomeTime event in metronome.cs
    private void OnMetronomeTime(float beatFloat, double songTime)
    {
        _songTime = songTime - songTimeOffset;
        _beat = beatFloat - beatOffset;

        if (!_gotTransform && _beat > getTransform)
        {
            _basePosition = Player.Instance.transform.position + new Vector3(0, heightOffset, 0);
            _baseRotation = Player.Instance.transform.rotation;

            _gotTransform = true;
        }

        ProcessDueEvents();
    }

    // main logic loop, runs on every update to OnMetronomeTime
    private void ProcessDueEvents()
    {
        while (HasMoreEvents() && IsNextEventDue())
        {
            Schedule.Event evt = _events[_nextIndex];

            if (evt.item == null)
            {
                Debug.LogWarning($"Scheduler: skipping invalid event {_nextIndex} in schedule {schedule?.name ?? "<none>"} because its item reference is missing or destroyed.");
                _nextIndex++;
                continue;
            }

            SpawnEvent(evt);
            _nextIndex++;
        }
    }

    //####### HELPER FUNCTIONS / UTILITY FUNCTIONS #######//
    public void LoadSchedule(Schedule s)
    {
        schedule = s;
        ConfigureLoadedSchedule();
    }

    private void ConfigureLoadedSchedule()
    {
        _tutorialReplayBeat = -1f;
        _tutorialTrackingPoints = false;
        _tutorialAttemptNumber = 0;
        _tutorialCheckpointIndex = -1;
        _tutorialRepeatStartTime = 0d;
        _tutorialRepeatEndTime = 0d;
        _tutorialTrackingStartTime = 0d;
        _tutorialCurrentRequiredPoints = 0f;
        _tutorialCurrentScheduleName = "<none>";
        _tutorialGateEvents = new();
        _tutorialRepeatEvents = new();
        _tutorialTailEvents = new();
        _tutorialMasterEvents = new();
        _tutorialCheckpoints = new();

        if (!tutorialMode)
        {
            _tutorialState = TutorialState.Disabled;
            SetActiveEvents(FlattenScheduleToAbsoluteTime(schedule));
            return;
        }

        ConfigureTutorialSegments();
    }

    private void ConfigureTutorialSegments()
    {
        _tutorialMasterEvents = FlattenScheduleToAbsoluteTime(schedule);

        if (_tutorialMasterEvents.Count == 0)
        {
            _tutorialState = TutorialState.Complete;
            SetActiveEvents(new List<Schedule.Event>());
            Debug.LogWarning($"Scheduler: tutorial mode is enabled, but schedule '{schedule?.name ?? "<none>"}' has no playable events.");
            return;
        }

        _tutorialCheckpoints = BuildTutorialCheckpoints(_tutorialMasterEvents);

        if (_tutorialCheckpoints.Count == 0)
        {
            ConfigureFallbackTutorial();
            return;
        }

        double masterEndTime = GetSegmentDuration(_tutorialMasterEvents);
        double tailStartTime = _tutorialCheckpoints[_tutorialCheckpoints.Count - 1].repeatEndTime;
        _tutorialTailEvents = BuildSegment(_tutorialMasterEvents, tailStartTime, masterEndTime, includeStartBoundary: false).events;

        Debug.Log($"Scheduler: configured {_tutorialCheckpoints.Count} tutorial checkpoints in '{schedule?.name ?? "<none>"}'.");
        StartTutorialCheckpoint(0, resetClock: false);
    }

    private void ConfigureFallbackTutorial()
    {
        double masterEndTime = GetSegmentDuration(_tutorialMasterEvents);
        TutorialSegment fullScheduleSegment = BuildSegment(_tutorialMasterEvents, 0d, masterEndTime, includeStartBoundary: true);

        _tutorialGateEvents = fullScheduleSegment.events;
        _tutorialRepeatEvents = fullScheduleSegment.events;
        _tutorialTailEvents = new();
        _tutorialRepeatStartTime = 0d;
        _tutorialRepeatEndTime = masterEndTime;
        _tutorialTrackingStartTime = 0d;
        _tutorialCurrentRequiredPoints = GetRequiredPointsForSchedule(0);
        _tutorialCurrentScheduleName = schedule?.name ?? "<none>";

        SetActiveEvents(_tutorialGateEvents);
        _tutorialState = _tutorialGateEvents.Count == 0 ? TutorialState.Complete : TutorialState.PlayingMasterUntilGate;
        _tutorialAttemptNumber = _tutorialState == TutorialState.Complete ? 0 : 1;

        Debug.Log(
            $"Scheduler: no valid tutorial checkpoints were configured for '{schedule?.name ?? "<none>"}'. " +
            $"Falling back to repeating the full schedule with {_tutorialCurrentRequiredPoints} required points.");

        if (_tutorialTrackingStartTime <= 0d)
        {
            StartTrackingTutorialPoints();
        }
    }

    private List<TutorialCheckpoint> BuildTutorialCheckpoints(List<Schedule.Event> masterEvents)
    {
        List<TutorialCheckpoint> checkpoints = new();
        List<TutorialScheduleOccurrence> occurrences = new();
        CollectScheduleOccurrences(schedule, 0d, occurrences);
        occurrences.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        if (tutorialRepeatSchedules.Count == 0)
        {
            return checkpoints;
        }

        if (tutorialRequiredPointsBySchedule.Count != tutorialRepeatSchedules.Count)
        {
            Debug.LogWarning(
                $"Scheduler: tutorial repeat schedule count ({tutorialRepeatSchedules.Count}) does not match required points count ({tutorialRequiredPointsBySchedule.Count}). " +
                $"Missing point entries will use the fallback value {tutorialFallbackRequiredPoints}.");
        }

        double gateStartTime = 0d;

        for (int i = 0; i < tutorialRepeatSchedules.Count; i++)
        {
            Schedule targetSchedule = tutorialRepeatSchedules[i];

            if (targetSchedule == null)
            {
                Debug.LogWarning($"Scheduler: tutorial repeat schedule at index {i} is null, skipping.");
                continue;
            }

            if (!TryFindNextScheduleOccurrence(occurrences, targetSchedule, gateStartTime, out TutorialScheduleOccurrence occurrence))
            {
                Debug.LogWarning(
                    $"Scheduler: tutorial repeat schedule '{targetSchedule.name}' could not be matched after {gateStartTime:F2}s inside '{schedule?.name ?? "<none>"}'. " +
                    $"Discovered nested schedules: {DescribeNestedSchedules(schedule)}.");
                continue;
            }

            TutorialSegment repeatSegment = BuildSegment(masterEvents, occurrence.startTime, occurrence.endTime, includeStartBoundary: true);
            if (repeatSegment.events.Count == 0)
            {
                Debug.LogWarning(
                    $"Scheduler: tutorial repeat schedule '{targetSchedule.name}' matched at {occurrence.startTime:F2}s, but it did not produce any replayable events.");
                continue;
            }

            bool includeGateStartBoundary = checkpoints.Count == 0;
            TutorialSegment gateSegment = BuildSegment(masterEvents, gateStartTime, occurrence.endTime, includeGateStartBoundary);
            TutorialCheckpoint checkpoint = new TutorialCheckpoint
            {
                schedule = targetSchedule,
                requiredPoints = GetRequiredPointsForSchedule(i),
                gateStartTime = gateStartTime,
                repeatStartTime = occurrence.startTime,
                repeatEndTime = occurrence.endTime,
                gateTrackingStartTime = occurrence.startTime - repeatSegment.leadIn - gateStartTime + gateSegment.leadIn,
                gateEvents = gateSegment.events,
                repeatEvents = repeatSegment.events,
            };

            checkpoints.Add(checkpoint);
            gateStartTime = occurrence.endTime;
        }

        return checkpoints;
    }

    private bool TryFindNextScheduleOccurrence(List<TutorialScheduleOccurrence> occurrences, Schedule targetSchedule, double minimumStartTime, out TutorialScheduleOccurrence occurrence)
    {
        const double timeTolerance = 0.0001d;

        foreach (TutorialScheduleOccurrence candidate in occurrences)
        {
            if (candidate.startTime + timeTolerance < minimumStartTime) continue;

            if (NamesMatch(candidate.schedule?.name, targetSchedule?.name) || SchedulesMatch(candidate.schedule, targetSchedule))
            {
                occurrence = candidate;
                return true;
            }
        }

        occurrence = null;
        return false;
    }

    private void StartTutorialCheckpoint(int checkpointIndex, bool resetClock)
    {
        if (checkpointIndex < 0 || checkpointIndex >= _tutorialCheckpoints.Count)
        {
            _tutorialState = TutorialState.Complete;
            return;
        }

        TutorialCheckpoint checkpoint = _tutorialCheckpoints[checkpointIndex];
        _tutorialCheckpointIndex = checkpointIndex;
        _tutorialGateEvents = checkpoint.gateEvents;
        _tutorialRepeatEvents = checkpoint.repeatEvents;
        _tutorialRepeatStartTime = checkpoint.repeatStartTime;
        _tutorialRepeatEndTime = checkpoint.repeatEndTime;
        _tutorialTrackingStartTime = checkpoint.gateTrackingStartTime;
        _tutorialCurrentRequiredPoints = checkpoint.requiredPoints;
        _tutorialCurrentScheduleName = checkpoint.Name;
        _tutorialTrackingPoints = false;
        _tutorialReplayBeat = -1f;
        _tutorialAttemptNumber = 1;

        SetActiveEvents(_tutorialGateEvents);

        if (resetClock)
        {
            Reset();
        }

        _tutorialState = _tutorialGateEvents.Count == 0 ? TutorialState.Complete : TutorialState.PlayingMasterUntilGate;

        Debug.Log(
            $"Scheduler: tutorial checkpoint {checkpointIndex + 1}/{_tutorialCheckpoints.Count} '{_tutorialCurrentScheduleName}' " +
            $"requires {_tutorialCurrentRequiredPoints} points. Repeat window: {_tutorialRepeatStartTime:F2}s to {_tutorialRepeatEndTime:F2}s.");

        if (_tutorialTrackingStartTime <= 0d)
        {
            StartTrackingTutorialPoints();
        }
    }

    // resets the schedule
    public void Reset()
    {
        _nextIndex = 0;

        // Set beat offset so that old events may fire again
        beatOffset += _beat;

        // Time should be aligned to beats
        songTimeOffset += _metronome.BeatsToTime(_beat);

        _songTime = 0d;
        _beat = 0f;
    }

    // does this have more events to initialize?
    public bool HasMoreEvents()
    {
        if (schedule == null) return false;

        return _nextIndex < _events.Count;
    }

    // checks if event is due to be scheduled in reference to current song time and schedule ahead time
    private bool IsNextEventDue()
    {
        if (schedule == null) return false;

        Schedule.Event evt = _events[_nextIndex];

        if (evt.item == null) return true;

        return GetScheduledTime(evt) - evt.item.scheduleAhead <= _songTime;
    }

    // spawns a gameObject based off of scheduled event information
    private void SpawnEvent(Schedule.Event evt)
    {
        Vector3 worldPos = _basePosition + _baseRotation * evt.position;
        Quaternion worldRot = _baseRotation * evt.rotation;

        Schedulable obj = Instantiate(evt.item, worldPos, worldRot);

        if (evt.useScale) obj.transform.localScale = evt.scale;

        double late = _songTime - (GetScheduledTime(evt) - evt.item.scheduleAhead);
        obj.startTime = Time.time - (float)late;
    }

    private double GetScheduledTime(Schedule.Event evt)
    {
        if (evt.useTime) return evt.time;
        else return _metronome.BeatsToTime(evt.beat - 1); // Beats start at 1
    }

    private void FinishTutorialAttempt()
    {
        if (_streakTracker == null)
        {
            _tutorialReplayBeat = -1f;
            Debug.LogWarning("Scheduler tutorial mode requires a StreakTracker component.");
            ContinueAfterTutorialPass();
            return;
        }

        if (_streakTracker.streak >= _tutorialCurrentRequiredPoints)
        {
            _tutorialReplayBeat = -1f;
            Debug.Log(
                $"Scheduler: tutorial passed on attempt {_tutorialAttemptNumber} with {_streakTracker.streak} / {_tutorialCurrentRequiredPoints} points " +
                $"while repeating '{_tutorialCurrentScheduleName}'.");
            ContinueAfterTutorialPass();
            return;
        }

        Debug.Log(
            $"Scheduler: tutorial failed on attempt {_tutorialAttemptNumber} with {_streakTracker.streak} / {_tutorialCurrentRequiredPoints} points. " +
            $"Replaying '{_tutorialCurrentScheduleName}' after {tutorialReplayDelayBeats} beats.");
        _streakTracker.streak = 0f;
        _tutorialTrackingPoints = false;
        _tutorialState = TutorialState.WaitingToReplay;
        _tutorialReplayBeat = _beat + Mathf.Max(0f, tutorialReplayDelayBeats);

        if (tutorialReplayDelayBeats <= 0f)
        {
            StartTutorialRepeatAttempt();
        }
    }

    private void StartTutorialRepeatAttempt()
    {
        if (_tutorialRepeatEvents.Count == 0)
        {
            ContinueAfterTutorialPass();
            return;
        }

        _tutorialReplayBeat = -1f;
        _tutorialAttemptNumber++;
        _tutorialTrackingPoints = false;
        SetActiveEvents(_tutorialRepeatEvents);
        Reset();
        _tutorialState = TutorialState.PlayingRepeat;
        StartTrackingTutorialPoints();

        Debug.Log(
            $"Scheduler: starting tutorial replay attempt {_tutorialAttemptNumber} for '{_tutorialCurrentScheduleName}'. " +
            $"Replay events: {_tutorialRepeatEvents.Count}.");
    }

    private void ContinueAfterTutorialPass()
    {
        _tutorialReplayBeat = -1f;
        _tutorialTrackingPoints = false;

        int nextCheckpointIndex = _tutorialCheckpointIndex + 1;
        if (_tutorialCheckpointIndex >= 0 && nextCheckpointIndex < _tutorialCheckpoints.Count)
        {
            StartTutorialCheckpoint(nextCheckpointIndex, resetClock: true);
            return;
        }

        if (_tutorialTailEvents.Count == 0)
        {
            _tutorialState = TutorialState.Complete;
            Debug.Log("Scheduler: tutorial complete, no remaining tail events to continue.");
            return;
        }

        SetActiveEvents(_tutorialTailEvents);
        Reset();
        _tutorialState = TutorialState.PlayingTail;
        Debug.Log($"Scheduler: tutorial complete, continuing into the remaining master schedule with {_tutorialTailEvents.Count} events.");
    }

    private void ActivateTutorialPointTrackingIfNeeded()
    {
        if (_tutorialTrackingPoints) return;
        if (_tutorialState != TutorialState.PlayingMasterUntilGate) return;
        if (_songTime < _tutorialTrackingStartTime) return;

        StartTrackingTutorialPoints();
    }

    private void StartTrackingTutorialPoints()
    {
        _tutorialTrackingPoints = true;

        if (_streakTracker != null)
        {
            _streakTracker.streak = 0f;
        }

        Debug.Log(
            $"Scheduler: tutorial point tracking started for '{_tutorialCurrentScheduleName}' " +
            $"at beat {_beat:F2}, song time {_songTime:F2}s.");
    }

    private bool HasActiveMovementCues()
    {
        return FindObjectsOfType<MovementCue>().Length > 0;
    }

    private void SetActiveEvents(List<Schedule.Event> events)
    {
        _events = events == null ? new List<Schedule.Event>() : new List<Schedule.Event>(events);
        _nextIndex = 0;
    }

    private TutorialSegment BuildSegment(List<Schedule.Event> masterEvents, double segmentStartTime, double segmentEndTime, bool includeStartBoundary)
    {
        const double timeTolerance = 0.0001d;

        List<Schedule.Event> sourceEvents = new();

        foreach (Schedule.Event evt in masterEvents)
        {
            if (includeStartBoundary)
            {
                if (evt.time + timeTolerance < segmentStartTime) continue;
            }
            else
            {
                if (evt.time <= segmentStartTime + timeTolerance) continue;
            }

            if (evt.time > segmentEndTime + timeTolerance) continue;

            sourceEvents.Add(evt);
        }

        double leadIn = GetSegmentLeadIn(sourceEvents, segmentStartTime);
        return new TutorialSegment
        {
            leadIn = leadIn,
            events = BuildRelativeSegment(sourceEvents, segmentStartTime, leadIn),
        };
    }

    private List<Schedule.Event> BuildRelativeSegment(List<Schedule.Event> sourceEvents, double segmentStartTime, double leadIn)
    {
        List<Schedule.Event> relativeEvents = new();

        foreach (Schedule.Event sourceEvent in sourceEvents)
        {
            Schedule.Event copy = sourceEvent.CreateRuntimeCopy();
            copy.beat = 1f;
            copy.time = (float)Math.Max(0d, sourceEvent.time - segmentStartTime + leadIn);
            relativeEvents.Add(copy);
        }

        relativeEvents.Sort((e1, e2) => e1.time.CompareTo(e2.time));
        return relativeEvents;
    }

    private double GetSegmentLeadIn(List<Schedule.Event> sourceEvents, double segmentStartTime)
    {
        bool foundEvent = false;
        double earliestDueOffset = 0d;

        foreach (Schedule.Event sourceEvent in sourceEvents)
        {
            double dueOffset = sourceEvent.time - segmentStartTime - (sourceEvent.item?.scheduleAhead ?? 0f);

            if (!foundEvent || dueOffset < earliestDueOffset)
            {
                earliestDueOffset = dueOffset;
                foundEvent = true;
            }
        }

        return foundEvent ? Math.Max(0d, -earliestDueOffset) : 0d;
    }

    private List<Schedule.Event> FlattenScheduleToAbsoluteTime(Schedule source)
    {
        List<Schedule.Event> flattenedEvents = new();
        FlattenScheduleInto(source, 0d, Vector3.zero, Vector3.one, Quaternion.identity, flattenedEvents);
        flattenedEvents.Sort((e1, e2) => e1.time.CompareTo(e2.time));
        return flattenedEvents;
    }

    private void FlattenScheduleInto(Schedule source, double timeOffset, Vector3 positionOffset, Vector3 scaleOffset, Quaternion rotationOffset, List<Schedule.Event> flattenedEvents)
    {
        if (source == null) return;

        foreach (Schedule.Event evt in source.rawEvents)
        {
            double eventTime = timeOffset + GetCanonicalTime(evt);
            Vector3 eventPosition = evt.position + positionOffset;
            Vector3 eventScale = Vector3.Scale(evt.scale, scaleOffset);
            Quaternion eventRotation = evt.rotation * rotationOffset;

            if (evt.subschedule != null)
            {
                FlattenScheduleInto(evt.subschedule, eventTime, eventPosition, eventScale, eventRotation, flattenedEvents);
                continue;
            }

            Schedule.Event copy = evt.CreateRuntimeCopy();
            copy.position = eventPosition;
            copy.scale = eventScale;
            copy.rotation = eventRotation;
            copy.beat = 1f;
            copy.time = (float)eventTime;
            flattenedEvents.Add(copy);
        }
    }

    private void CollectScheduleOccurrences(Schedule source, double timeOffset, List<TutorialScheduleOccurrence> occurrences)
    {
        if (source == null) return;

        foreach (Schedule.Event evt in source.rawEvents)
        {
            if (evt.subschedule == null) continue;

            double startTime = CanonicalizeEventTime(timeOffset + GetCanonicalTime(evt));
            double endTime = CanonicalizeEventTime(startTime + GetScheduleDuration(evt.subschedule));

            occurrences.Add(new TutorialScheduleOccurrence
            {
                schedule = evt.subschedule,
                startTime = startTime,
                endTime = endTime,
            });

            CollectScheduleOccurrences(evt.subschedule, startTime, occurrences);
        }
    }

    private bool SchedulesMatch(Schedule source, Schedule target)
    {
        if (source == target) return true;
        if (source == null || target == null) return source == target;
        if (!NamesMatch(source.name, target.name)) return false;

        IReadOnlyList<Schedule.Event> sourceEvents = source.rawEvents;
        IReadOnlyList<Schedule.Event> targetEvents = target.rawEvents;

        if (sourceEvents.Count != targetEvents.Count) return false;

        for (int i = 0; i < sourceEvents.Count; i++)
        {
            if (!EventsMatch(sourceEvents[i], targetEvents[i])) return false;
        }

        return true;
    }

    private bool EventsMatch(Schedule.Event source, Schedule.Event target)
    {
        if (!Mathf.Approximately(source.beat, target.beat)) return false;
        if (!Mathf.Approximately(source.time, target.time)) return false;
        if (!VectorsApproximatelyEqual(source.position, target.position)) return false;
        if (!VectorsApproximatelyEqual(source.scale, target.scale)) return false;
        if (!QuaternionsApproximatelyEqual(source.rotation, target.rotation)) return false;
        if (!SchedulablesMatch(source.item, target.item)) return false;
        if (!SchedulesMatch(source.subschedule, target.subschedule)) return false;

        return true;
    }

    private bool SchedulablesMatch(Schedulable source, Schedulable target)
    {
        if (source == target) return true;
        if (source == null || target == null) return source == target;

        return NamesMatch(source.name, target.name);
    }

    private bool NamesMatch(string source, string target)
    {
        return string.Equals(NormalizeName(source), NormalizeName(target), StringComparison.OrdinalIgnoreCase);
    }

    private string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        string normalized = value.Trim();
        int atIndex = normalized.IndexOf('@');
        if (atIndex >= 0)
        {
            normalized = normalized.Substring(0, atIndex);
        }

        const string cloneSuffix = "(Clone)";
        if (normalized.EndsWith(cloneSuffix, StringComparison.Ordinal))
        {
            normalized = normalized.Substring(0, normalized.Length - cloneSuffix.Length).TrimEnd();
        }

        return normalized.Trim();
    }

    private bool VectorsApproximatelyEqual(Vector3 source, Vector3 target)
    {
        return Mathf.Approximately(source.x, target.x)
            && Mathf.Approximately(source.y, target.y)
            && Mathf.Approximately(source.z, target.z);
    }

    private bool QuaternionsApproximatelyEqual(Quaternion source, Quaternion target)
    {
        return Mathf.Approximately(source.x, target.x)
            && Mathf.Approximately(source.y, target.y)
            && Mathf.Approximately(source.z, target.z)
            && Mathf.Approximately(source.w, target.w);
    }

    private string DescribeNestedSchedules(Schedule source)
    {
        if (source == null) return "<none>";

        List<string> descriptions = new();
        CollectNestedScheduleDescriptions(source, 0d, descriptions);
        return descriptions.Count == 0 ? "<none>" : string.Join(", ", descriptions);
    }

    private void CollectNestedScheduleDescriptions(Schedule source, double timeOffset, List<string> descriptions)
    {
        if (source == null) return;

        foreach (Schedule.Event evt in source.rawEvents)
        {
            if (evt.subschedule == null) continue;

            double startTime = timeOffset + GetCanonicalTime(evt);
            descriptions.Add($"{evt.subschedule.name}@{startTime:F2}s");
            CollectNestedScheduleDescriptions(evt.subschedule, startTime, descriptions);
        }
    }

    private float GetRequiredPointsForSchedule(int scheduleIndex)
    {
        if (scheduleIndex >= 0 && scheduleIndex < tutorialRequiredPointsBySchedule.Count)
        {
            return tutorialRequiredPointsBySchedule[scheduleIndex];
        }

        return tutorialFallbackRequiredPoints;
    }

    private double GetScheduleDuration(Schedule source)
    {
        return GetSegmentDuration(FlattenScheduleToAbsoluteTime(source));
    }

    private double CanonicalizeEventTime(double time)
    {
        return (double)(float)time;
    }

    private double GetSegmentDuration(List<Schedule.Event> events)
    {
        double duration = 0d;

        foreach (Schedule.Event evt in events)
        {
            duration = Math.Max(duration, evt.time);
        }

        return duration;
    }

    private double GetCanonicalTime(Schedule.Event evt)
    {
        return evt.useTime ? evt.time : _metronome.BeatsToTime(evt.beat - 1f);
    }
}
