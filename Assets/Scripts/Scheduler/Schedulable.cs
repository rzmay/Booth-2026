using System.Collections.Generic;
using UnityEngine;

public interface ISchedulable
{
    float startTime { get; set; }
    float scheduleAhead { get; }
}

public abstract class Schedulable : MonoBehaviour, ISchedulable
{
    // Start time needs to be set precisely on spawn
    [HideInInspector] public float startTime { get; set; }

    // How long to spawn before specified time
    virtual public float scheduleAhead { get; }
}
