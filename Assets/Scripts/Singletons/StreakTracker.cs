using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

using UnityEngine;

public class StreakTracker : MonoBehaviour
{
    public static StreakTracker Instance;

    private SongData _song = null;
    [SerializeField] private float _streak = 0f; // Private, but show in editor for debugging
    [SerializeField] private TMP_Text _scoreLabel;
    private float _score = 0f;
    public float score
    {
        get { return _score; }
        private set { _score = Mathf.Max(value, 0); } // Score has no max value
    }


    public float streak
    {
        get { return _streak; }
        set { _streak = Mathf.Clamp(value, 0, _song?.maxStreak ?? 0); }
    }

    // Returns a float array of each progress
    public float[] streakProgresses
    {
        get
        {
            if (_song == null) return new float[4];

            float[] arr = new float[4];

            for (int i = 0; i < _song.trackThresholds.Length; i++)
            {
                float lowerBound = i == 0 ? 0 : _song.trackThresholds[i - 1];
                float upperBound = _song.trackThresholds[i];

                float t = Mathf.InverseLerp(lowerBound, upperBound, _streak);

                arr[i] = t;
            }

            return arr;
        }
    }

    // Returns a float 0 - 4 representing progress through threshholds. Use for graphics
    public float streakProgress { get { return streakProgresses.Sum(); } }

    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (_scoreLabel != null) _scoreLabel.text = Mathf.Ceil(_score).ToString();
    }

    void _TrackCue(MovementCue.Result result, int level = 0)
    {
        if (_song == null) return;

        int i = Mathf.Clamp(level, 0, _song.streakScoring.Count - 1);
        streak += _song.streakScoring[i][result];
        score += _song.streakScoring[i][result] * (i + 1); // Scoring scales more with harder cues
    }

    public void LoadSongData(SongData songData)
    {
        _song = songData;
    }

    public static void TrackCue(MovementCue.Result result, int level = 0)
    {
        Instance._TrackCue(result, level);
    }
}
