using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MovementCueVisualizer))]
public class MovementCue : Schedulable
{
    private static Queue<MovementCue> _Queue = new();

    private static void PruneQueue()
    {
        _Queue = new Queue<MovementCue>(_Queue.Where(e => e != null));
    }

    public enum Result
    {
        Miss,
        Early,
        OnTime,
        Late,
        Perfect,
    }

    [System.Serializable]
    public class ResultMap<T>
    {
        public T miss;
        public T early;
        public T late;
        public T onTime;
        public T perfect;

        public T this[Result key]
        {
            get
            {
                switch (key)
                {
                    case Result.Miss:
                        return miss;
                    case Result.Early:
                        return early;
                    case Result.Late:
                        return late;
                    case Result.OnTime:
                        return onTime;
                    case Result.Perfect:
                        return perfect;
                    default:
                        return miss;
                }
            }
        }
    }

    // Which hand is this for
    public Hand.Side hand;

    // Used for streak tracking
    public int level = 0;

    /* The cue should be open for the entire hit window, the sum of earlyWindow and lateWindow.
    * the "On time" and "Perfect" window will be determined by a set threshhold, equal between the early and late.
    * These settings should be pretty easy so kids can have fun
    */
    public float earlyWindowBeats = 1.5f;
    public float lateWindowBeats = 1f;
    public float onTimeWindow = 0.2f;
    public float perfectWindow = 0.1f;

    // earlyWindow and lateWindow should be beats rather than seconds
    public float earlyWindow { get { return (float)MusicManager.Metronome?.BeatsToTime(earlyWindowBeats); } }
    public float lateWindow { get { return (float)MusicManager.Metronome?.BeatsToTime(lateWindowBeats); } }


    // TODO: Calibrate this to a reasonable value
    public float hitRadius = 0.2f;

    private float _time;
    private bool _hit = false;

    // Getter variables determined by the windows in order to make graphics easier
    public float hitWindow { get { return earlyWindow + lateWindow; } }

    // 0-1 = before perfect, 1-2 = after perfect
    public float hitWindowProgress
    {
        get
        {
            float earlyProgress = Mathf.InverseLerp(0, earlyWindow, _time);
            float lateProgress = Mathf.InverseLerp(earlyWindow, earlyWindow + lateWindow, _time);

            return Mathf.Clamp(earlyProgress, 0, 1) + Mathf.Clamp(lateProgress, 0, 1);
        }
    }

    // Target time for this to be hit
    public float targetTime { get { return startTime + earlyWindow; } }

    // Is this next up? Multiple can be next if at the same time
    public bool isNext
    {
        get
        {
            PruneQueue();
            if (_Queue.Count == 0) return false;

            MovementCue next = _Queue.Peek();
            return next == this || next.targetTime == targetTime;
        }
    }

    [HideInInspector] override public float scheduleAhead { get { return earlyWindow; } }

    private MovementCueVisualizer _visualizer;
    private Detacher _detacher;
    private List<MovementCue> _previous;

    void Awake()
    {
        _visualizer = GetComponent<MovementCueVisualizer>();
        _detacher = GetComponent<Detacher>();
    }

    void Start()
    {
        PruneQueue();

        List<MovementCue> sideList = _Queue.Where(e => e.hand == hand).ToList();
        if (sideList.Count > 0)
        {
            _previous = sideList.Where(e => e.targetTime == sideList[^1].targetTime).ToList();
            _visualizer.ShowNextLine(_previous);
        }

        _Queue.Enqueue(this);
    }

    void OnDestroy()
    {
        if (_Queue.Count == 0) return;

        _Queue = new Queue<MovementCue>(_Queue.Where(e => e != null && e != this));
    }

    // Update is called once per frame
    void Update()
    {
        _time = Time.time - startTime;

        // Despawn if done
        if (_time > hitWindow)
        {
            TrackResult(Result.Miss);
        }

        // Check if the hands are in the right place
        if (!_hit) CheckHands();
    }


    void CheckHands()
    {
        Hand handObject = hand == Hand.Side.Left ? Player.Instance.leftHand : Player.Instance.rightHand;
        Vector3 pos = transform.position;
        float distance = Vector3.Distance(pos, handObject.transform.position);

        // Don't do anything if the hand isn't touching
        if (distance > hitRadius) return;

        // If we make it here, the hand is hitting the right spot
        _hit = true;

        // Was it timed within the on time / perfect windows?
        bool early = _time < earlyWindow - onTimeWindow;
        bool late = earlyWindow + onTimeWindow < _time;
        bool onTime = !(early || late);
        bool perfect = earlyWindow - perfectWindow < _time && _time < earlyWindow + perfectWindow;

        // Track
        Result result = onTime ?
            (perfect ? Result.Perfect : Result.OnTime) :
            (early ? Result.Early : Result.Late);

        TrackResult(result);
    }

    void TrackResult(Result result)
    {
        PruneQueue();

        // Can't dequeue, as the cue being removed may not actually be the oldest.
        // Instead remove specific element
        _Queue = new Queue<MovementCue>(_Queue.Where(e => e != this));

        StreakTracker.TrackCue(result, level);
        _visualizer.VisualizeResult(result);
        Player.Instance.hapticsController.Play(hand, result);

        // Detach and destroy
        _detacher.Detach();
        Destroy(gameObject);
    }
}
