using System;
using System.Collections.Generic;
using UnityEngine;

// SCHEDULER CLASS
// handles the scheduling of events based on the metronome's beat timing, allowing for precise timing of gameplay events and visual effects in sync with the music
[RequireComponent(typeof(Metronome))]
public class Scheduler : MonoBehaviour
{
    // SCHEDULER WORKFLOW:
    // 1. scheduler subscribes to the metronome events (VisualizerOnBeat and ScoringEvent) to receive beat timing information
    // 2. loads ScriptableObject schedules that has a list of events to be scheduled, each with specific timing and parameters
    // 3. keeps a pointer nextIndex into the list of events to always know the next event to be scheduled
    // 4. runs update loop. If an event is due to run on timestep curTime + scheduleAheadTime, then we instantiate the prefab and trigger the event, and move the pointer to the next event in the list. This allows us to schedule events slightly ahead of time to ensure they are triggered precisely on beat, even if there are frame rate drops or other performance issues.

    // VARIABLE INITIALIZATION //
    [SerializeField] public Schedule schedule;
    [SerializeField] private CalibrationManager calibrationManager;

    private Metronome _metronome;

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
    public float getTransform = -2f; // Legacy field; calibration is now captured on player input.
    public float heightOffset = -0.5f;
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private bool _gotTransform = false;
    private bool _usingCalibration = false;

    private List<Schedule.Event> _events = new();

    void Awake()
    {
        _metronome = GetComponent<Metronome>();
        calibrationManager = GetComponent<CalibrationManager>();
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

    // subscribed to OnMetronomeTime event in metronome.cs
    private void OnMetronomeTime(float beatFloat, double songTime)
    {
        _songTime = songTime - songTimeOffset;
        _beat = beatFloat - beatOffset;

        // check if we have more events to schedule
        if (!HasMoreEvents()) return;

        if (calibrationManager.CurrentCalibration.isCalibrated)
        {
            if (!_usingCalibration)
            {
                CalibrationData calibration = calibrationManager.CurrentCalibration;
                _basePosition = calibration.originPosition;
                _baseRotation = calibration.originRotation;
                _gotTransform = true;
                _usingCalibration = true;
            }
        }
        else
        {
            _basePosition = Player.Instance.transform.position + new Vector3(0, heightOffset, 0);
            _baseRotation = Player.Instance.transform.rotation;
            _gotTransform = true;
            _usingCalibration = false;
        }

        ProcessDueEvents();
    }

    // main logic loop, runs on every update to OnMetronomeTime
    private void ProcessDueEvents()
    {
        // catch-up loops are so tuff
        while (HasMoreEvents() && IsNextEventDue())
        {
            Schedule.Event evt = _events[_nextIndex];

            // Spawn the item associated with the event
            SpawnEvent(evt);

            // increment
            _nextIndex++;
        }
    }


    //####### HELPER FUNCTIONS / UTILITY FUNCTIONS #######//
    public void LoadSchedule(Schedule s)
    {
        schedule = s;

        // Cache and sort events
        _events = new(schedule.events);

        // Requires metronome initialization (e.g. bpm > 0)
        float bpm = _metronome.bpm;
        if (bpm > 0) _events.Sort((e1, e2) => e1.GetCanonTime(bpm).CompareTo(e2.GetCanonTime(bpm)));
    }

    // resets the schedule
    public void Reset()
    {
        _nextIndex = 0;

        // Set beat offset so that old events may fire again
        beatOffset += _beat - 1;

        // Time should be aligned to beats
        songTimeOffset += _metronome.BeatsToTime(Mathf.Floor(_beat));
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

        // check if event is due to be scheduled within the schedule ahead time
        Schedule.Event evt = _events[_nextIndex];

        // Use beats if time is negative, othterwise use time
        return GetScheduledTime(evt) - evt.item.scheduleAhead <= _songTime;
    }

    // spawns a gameObject based off of scheduled event information
    private void SpawnEvent(Schedule.Event evt)
    {
        bool useAuthoredTransform = evt.item is ScheduledText;
        if (useAuthoredTransform)
        {
            Vector3 worldPos = evt.position;
            Quaternion worldRot = evt.rotation;
        }
        else
        {
            Vector3 worldPos = calibrationManager.ConvertNormalizedToWorldPosition(evt.position);
            Quaternion worldRot = _baseRotation * evt.rotation;
        }

        Schedulable obj = Instantiate(evt.item, worldPos, worldRot);

        if (evt.useScale) obj.transform.localScale = evt.scale;

        // Calculate start time
        double late = _songTime - (GetScheduledTime(evt) - evt.item.scheduleAhead);

        // Start time is in the past
        if (late > 0)
        {
            Debug.LogWarning($"Event {evt} is late by {late} seconds. Consider increasing schedule ahead time.");
        }
        obj.startTime = Time.time - (float)late;
    }

    private double GetScheduledTime(Schedule.Event evt)
    {
        if (evt.useTime) return evt.time;
        else return _metronome.BeatsToTime(evt.beat - 1); // Beats start at 1
    }
}
