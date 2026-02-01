using System;
using System.Collections.Generic;
using UnityEngine;

public class StreakTracker : MonoBehaviour
{
    private static StreakTracker _Instance;

    [System.Serializable]
    public class Preset
    {
        public float miss = -1f;
        public float offTime = 0f;
        public float onTime = 1f;
        public float perfect = 2f;
    }

    [System.Serializable]
    public class SongPreset
    {
        public string song;

        // Each index represents a difficulty level for the motion cue, e.g. the 0th index is the easiest motion cue
        [SerializeField]
        public List<Dictionary<MovementCue.Result, float>> cueScores = new()
        {
            new Dictionary<MovementCue.Result, float>()
            {
                { MovementCue.Result.Miss, -1.0f },
                { MovementCue.Result.OffTime, 0f },
                { MovementCue.Result.OnTime, 1.0f },
                { MovementCue.Result.Perfect, 2.0f },
            }
        };

        // Streak thresholds for tracks to come in, final value = max streak
        [SerializeField] public float[] trackThresholds = new float[4];

        public float maxStreak { get { return trackThresholds[3]; } }
    }

    [SerializeField] private List<SongPreset> songs = new();


    private SongPreset _song = null;
    private float _streak = 0f;

    public float streak
    {
        get { return _streak; }
        set { _streak = Mathf.Clamp(value, 0, _song?.maxStreak ?? 0); }
    }

    // Returns a float 0 - 4 representing progress through threshholds. Use for graphics
    public float streakProgress
    {
        get
        {
            if (_song == null) return 0f;

            float sum = 0f;

            for (int i = 0; i < _song.trackThresholds.Length; i++)
            {
                float lowerBound = i == 0 ? 0 : _song.trackThresholds[i - 1];
                float upperBound = _song.trackThresholds[i];

                float t = Mathf.InverseLerp(lowerBound, upperBound, _streak);

                sum += Mathf.Clamp(t, 0, 1);
            }

            return sum;
        }
    }


    void Awake()
    {
        _Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void TrackCue(MovementCue.Result result, int level = 0)
    {
        if (_song == null) return;

        int i = Mathf.Clamp(level, 0, _song.cueScores.Count - 1);
        _streak += _song.cueScores[i][result];
    }
}
