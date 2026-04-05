using UnityEngine;

public class TutorialScheduler : MonoBehaviour
{
    // WORKFLOW
    // 1) get the different schedules from scriptable objects (SingleHit, PerfectHit, TripleHit, TwoHandsHit) 
    // 2) schedule the first event. When the event is completed (aka the player is done with the first event), schedule the next event. Repeat until all events are scheduled.
    // 3) when the player is done with all events, end the tutorial and transition to the main game

    // var initiation
    [SerializeField] private Schedule singleHitSchedule;
    [SerializeField] private Schedule perfectHitSchedule;
    [SerializeField] private Schedule tripleHitSchedule;
    [SerializeField] private Schedule twoHandsHitSchedule;
    private Schedule[] schedules;
    private int currentScheduleIndex = 0;

    private void Start()
    {
        schedules = new Schedule[] { singleHitSchedule, perfectHitSchedule, tripleHitSchedule, twoHandsHitSchedule };

        scheduler.schedule = schedules[currentScheduleIndex];
        scheduler.Reset();
    }

    private void Update()
    {
        // TODO:
        // check if the current event is completed. If it is, schedule next event.
    }




}
