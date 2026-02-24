using System;
using UnityEngine;

// SCHEDULER CLASS
// handles the scheduling of events based on the metronome's beat timing, allowing for precise timing of gameplay events and visual effects in sync with the music
public sealed class Scheduler : MonoBehaviour
{
    // SCHEDULER WORKFLOW:
    // 1. scheduler subscribes to the metronome events (VisualizerOnBeat and ScoringEvent) to receive beat timing information
    // 2. loads ScriptableObject schedules that has a list of events to be scheduled, each with specific timing and parameters
    // 3. keeps a pointer nextIndex into the list of events to always know the next event to be scheduled
    // 4. runs update loop. If an event is due to run on timestep curTime + scheduleAheadTime, then we instantiate the prefab and trigger the event, and move the pointer to the next event in the list. This allows us to schedule events slightly ahead of time to ensure they are triggered precisely on beat, even if there are frame rate drops or other performance issues.

    // VARIABLE INITIALIZATION //
    [SerializeField] private Metronome metronome;
    [SerializeField] private float scheduleAheadTime = 1.0f;

    [SerializeField] private Schedule schedule;

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

    public void Start()
    {
        if (metronome == null)
        {
            Debug.LogError("Metronome reference is not set in the Scheduler.");
            return;
        }

        if (Schedule == null)
        {
            Debug.LogError("Schedule reference is not set in the Scheduler.");
            return;
        }

        _nextIndex = 0;

        // subscribe to metronome events
        metronome.VisualizerOnBeat += OnBeat;
        metronome.ScoringEvent += OnScoringEvent;
    }

    // subscribe to metronome events
    void OnEnable()
    {
        metronome.VisualizerOnBeat += OnBeat;
        metronome.OnMetronomeTime += OnMetronomeTime;
    }

    // unsubscribe to metronome events
    void OnDisable()
    {
        metronome.VisualizerOnBeat -= OnBeat;
        metronome.OnMetronomeTime -= OnMetronomeTime;
    }

    // subscribed to OnMetronomeTime event in metronome.cs
    private void OnMetronomeTime(float beatFloat, double songTime)
    {
        // check if schedule is valid
        if (schedule == null) return;

        // check if we have more events to schedule
        if (nextIndex >= schedule.Events.Length) return;

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
            Schedule.Event evt = schedule.Events[nextScheduleIndex];

            // Spawn the item associated with the event
            SpawnEvent(evt);

            // increment
            nextIndex++;
        }
    }


    //####### HELPER FUNCTIONS / UTILITY FUNCTIONS #######//

    // resets the schedule
    public void ResetSchedule()
    {
        nextIndex = 0;

        // Set beat offset so that old events may fire again
        beatOffset += _beat;

        // Time should be aligned to beats
        songTimeOffset += metronome.BeatsToTime(_beat);
    }

    // does this have more events to initialize?
    public bool HasMoreEvents()
    {
        return nextIndex < schedule.Events.Length;
    }

    // checks if event is due to be scheduled in reference to current song time and schedule ahead time
    private float IsNextEventDue()
    {
        // check if event is due to be scheduled within the schedule ahead time
        Schedule.Event evt = schedule.events[nextIndex];

        // Use beats if time is negative, othterwise use time
        if (evt.time < 0) return evt.beat + metronome.TimeToBeats(evt.item.scheduleAhead) <= _beat;
        else return (double)(evt.time + evt.item.scheduleAhead) <= _songTime;
    }

    // spawns a gameObject based off of scheduled event information
    private gameObject SpawnEvent(Schedule.Event evt)
    {
        GameObject obj = Instantiate(evt.item, evt.transform, Quaternion.identity);
        Schedulable schedulable = obj.GetComponent<Schedulable>();

        // Calculate start time
        double late = _songTime - (double)evt.time;

        // Start time is in the past
        schedulable.startTime = Time.time - (float)late;
    }
}
