using UnityEngine;

public abstract class Schedulable : MonoBehaviour
{
    // Start time needs to be set precisely on spawn
    [HideInInspector] public float startTime;

    // How long to spawn before specified time
    virtual public float scheduleAhead { get; }
}
