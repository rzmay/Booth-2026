using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SongData", menuName = "Scriptable Objects/SongData")]
public class SongData : ScriptableObject
{
  [System.Serializable]
  public class CueScoring
  {
    public float miss = -1f;
    public float offTime = 0f;
    public float onTime = 1f;
    public float perfect = 2f;

    public float this[MovementCue.Result key]
    {
      get
      {
        switch (key)
        {
          case MovementCue.Result.Miss:
            return miss;
          case MovementCue.Result.OffTime:
            return offTime;
          case MovementCue.Result.OnTime:
            return onTime;
          case MovementCue.Result.Perfect:
            return perfect;
          default:
            return miss;
        }
      }
    }
  }

  public string songName;
  public AudioClip[] tracks = new AudioClip[4];
  public float bpm = 120.0f;
  public int countInBeats = 8;
  public int beatsPerMeasure = 4;
  public int loopToBeats = 0;

  [SerializeField]
  public List<CueScoring> streakScoring = new();

  [SerializeField]
  public float[] trackThresholds = new float[4];

  public float maxStreak { get { return trackThresholds[3]; } }
}
