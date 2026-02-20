using UnityEngine;

public class MovementCue : MonoBehaviour
{
    public enum Result
    {
        Miss,
        OffTime,
        OnTime,
        Perfect,
    }

    // Which hand is this for
    public Hand.Side hand;

    // Used for streak tracking
    public int level = 0;

    /* The cue should be open for the entire hit window, the sum of earlyWindow and lateWindow.
    * the "On time" and "Perfect" window will be determined by a set threshhold, equal between the early and late.
    * These settings should be pretty easy so kids can have fun
    */
    public float earlyWindow = 1.5f;
    public float lateWindow = 1f;
    public float onTimeWindow = 0.2f;
    public float perfectWindow = 0.1f;

    // TODO: Calibrate this to a reasonable value
    public float hitRadius = 0.05f;

    // Start time needs to be set precisely so that the perfect timing is on beat
    [HideInInspector] public float startTime;
    private float _time;
    private bool _hit = false;

    // Getter variables determined by the windows in order to make graphics easier
    public float hitWindow { get { return earlyWindow + lateWindow; } }

    // 0-1 = before perfect, 1-2 = after perfect
    public float hitWindowProgress
    {
        get
        {
            float earlyProgress = Mathf.InverseLerp(_time - startTime, 0, earlyWindow);
            float lateProgress = Mathf.InverseLerp(_time - startTime, earlyWindow, earlyWindow + lateWindow);

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
        _time = Time.time - startTime;

        // Despawn if done
        if (_time > hitWindow)
        {
            StreakTracker.TrackCue(Result.Miss, level);
            Destroy(gameObject);
        }

        // Check if the hands are in the right place
        if (!_hit) CheckHands();
    }


    void CheckHands()
    {
        Hand handObject = hand == Hand.Side.Left ? Player.Instance.leftHand : Player.Instance.rightHand;

        float distance = Vector3.Distance(transform.position, handObject.transform.position);

        // Don't do anything if the hand isn't touching
        if (distance > hitRadius) return;

        // If we make it here, the hand is hitting the right spot
        _hit = true;

        // Was it timed within the on time / perfect windows?
        bool onTime = earlyWindow - onTimeWindow < _time && _time < earlyWindow + onTimeWindow;
        bool perfect = earlyWindow - perfectWindow < _time && _time < earlyWindow + perfectWindow;

        // Track
        Result result = onTime ? (perfect ? Result.Perfect : Result.OnTime) : Result.OffTime;
        StreakTracker.TrackCue(result, level);
    }
}
