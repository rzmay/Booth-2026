using System;
using UnityEngine;

// SCHEDULER CLASS
// handles the scheduling of events based on the metronome's beat timing, allowing for precise timing of gameplay events and visual effects in sync with the music
[RequireComponent(typeof(Metronome))]
public sealed class Scheduler : MonoBehaviour
{
    // SCHEDULER WORKFLOW:
    // 1. scheduler subscribes to the metronome events (VisualizerOnBeat and ScoringEvent) to receive beat timing information
    // 2. loads ScriptableObject schedules that has a list of events to be scheduled, each with specific timing and parameters
    // 3. keeps a pointer nextIndex into the list of events to always know the next event to be scheduled
    // 4. runs update loop. If an event is due to run on timestep curTime + scheduleAheadTime, then we instantiate the prefab and trigger the event, and move the pointer to the next event in the list. This allows us to schedule events slightly ahead of time to ensure they are triggered precisely on beat, even if there are frame rate drops or other performance issues.

    // VARIABLE INITIALIZATION //
    [SerializeField] public Schedule schedule;

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

    void Awake()
    {
        _metronome = GetComponent<Metronome>();
    }

    public void Start()
    {
        _nextIndex = 0;

        // subscribe to metronome events
        _metronome.OnMetronomeTime += OnMetronomeTime;
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
        // check if we have more events to schedule
        if (!HasMoreEvents()) return;

        _songTime = songTime - songTimeOffset;
        _beat = beatFloat - beatOffset;

        ProcessDueEvents();
    }

    // main logic loop, runs on every update to OnMetronomeTime
    private void ProcessDueEvents()
    {
        // catch-up loops are so tuff
        while (HasMoreEvents() && IsNextEventDue())
        {
            Schedule.Event evt = schedule.events[_nextIndex];

            // Spawn the item associated with the event
            SpawnEvent(evt);

            // increment
            _nextIndex++;
        }
    }


    //####### HELPER FUNCTIONS / UTILITY FUNCTIONS #######//

    // resets the schedule
    public void Reset()
    {
        _nextIndex = 0;

        // Set beat offset so that old events may fire again
        beatOffset += _beat;

        // Time should be aligned to beats
        songTimeOffset += _metronome.BeatsToTime(_beat);
    }

    // does this have more events to initialize?
    public bool HasMoreEvents()
    {
        if (schedule == null) return false;

        return _nextIndex < schedule.events.Count;
    }

    // checks if event is due to be scheduled in reference to current song time and schedule ahead time
    private bool IsNextEventDue()
    {
        if (schedule == null) return false;

        // check if event is due to be scheduled within the schedule ahead time
        Schedule.Event evt = schedule.events[_nextIndex];

        // Use beats if time is negative, othterwise use time
        return GetScheduledTime(evt) - evt.item.scheduleAhead <= _songTime;
    }

    // spawns a gameObject based off of scheduled event information
    private void SpawnEvent(Schedule.Event evt)
    {
        Schedulable obj = Instantiate(evt.item, evt.position, evt.rotation);
        obj.transform.localScale = evt.scale;

        // Calculate start time
        double late = _songTime - (GetScheduledTime(evt) - evt.item.scheduleAhead);

        // Start time is in the past
        obj.startTime = Time.time - (float)late;
    }

    private double GetScheduledTime(Schedule.Event evt)
    {
        if (evt.time < 0) return _metronome.BeatsToTime(evt.beat - 1); // Beats start at 1
        else return evt.time;
    }
}
