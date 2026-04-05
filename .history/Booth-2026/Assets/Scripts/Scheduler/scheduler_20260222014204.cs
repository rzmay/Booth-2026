using UnityEngine;
using System;

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

    // RENAME THIS TO WHATEVER TEH SCRIPTABLE OBJECT SCHEDULES ARE CALLE
    [SerializeField] private Schedulable Schedule;

    // list of schedule events
    // RENAME or CHANGE THIS TOO
    //private Schedule.Event[] events;

    private int nextIndex = 0;

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

        nextIndex = 0;

        // subscribe to metronome events
        metronome.VisualizerOnBeat += OnBeat;
        metronome.ScoringEvent += OnScoringEvent;

        // load schedule events from the ScriptableObject
        // REPLACE THIS
        //events = Schedule.GetEvents();
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

    private void OnMetronomeTime(float beatFloat, double songTime)
    {

    }









}