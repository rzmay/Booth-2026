using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MovementCue : MonoBehaviour
{
    public enum Result
    {
        Miss,
        OffTime,
        OnTime,
        Perfect,
    }

    // Used for streak tracking
    public int level = 0;

    /* The cue should be open for the entire hit window, the sum of earlyWindow and lateWindow.
    * the "Perfect" window will be determined by a set threshhold, equal between the early and late.
    * These settings should be pretty easy so kids can have fun
    */
    public float earlyWindow = 1.5f;
    public float lateWindow = 1f;
    public float perfectWindow = 0.1f;

    // Track the time this has been active
    private float _startTime;
    private float _time;

    // Getter variables determined by the windows in order to make graphics easier
    public float hitWindow { get { return earlyWindow + lateWindow; } }

    // 0-1 = before perfect, 1-2 = after perfect
    public float hitWindowProgress
    {
        get
        {
            float earlyProgress = Mathf.InverseLerp(_time - _startTime, 0, earlyWindow);
            float lateProgress = Mathf.InverseLerp(_time - _startTime, earlyWindow, earlyWindow + lateWindow);

            return Mathf.Clamp(earlyProgress, 0, 1) + Mathf.Clamp(lateProgress, 0, 1);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    void OnCollisionEnter(Collision collision)
    {

    }
}
