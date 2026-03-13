using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SongData", menuName = "Scriptable Objects/SongData")]
public class SongData : ScriptableObject
{
  public string songName;

  [Header("Song Information")]

  public AudioClip[] tracks = new AudioClip[4];
  public float bpm = 120.0f;
  public int countInBeats = 8;
  public int beatsPerMeasure = 4;
  public int loopToBeats = 0;

  [Header("Scoring")]

  [SerializeField]
  public List<MovementCue.ResultMap<float>> streakScoring = new();
  [SerializeField]
  public float[] trackThresholds = new float[4];
  public float maxStreak { get { return trackThresholds[3]; } }

  [Header("Scheduling")]
  public Schedule schedule;
}
