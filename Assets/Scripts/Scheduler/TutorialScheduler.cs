using UnityEngine;

public class TutorialScheduler : Scheduler
{
    [Header("Tutorial")]
    [SerializeField] private Schedule tutorialSchedule;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    public void BeginTutorial()
    {
    }

    public void EndTutorial()
    {
    }

    public void LoadTutorialSchedule()
    {
        LoadSchedule(tutorialSchedule);
    }
}
