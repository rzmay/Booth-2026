using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Schedule", menuName = "Scriptable Objects/Schedule")]
public class Schedule : ScriptableObject
{
  [System.Serializable]
  public class Event
  {
    [Header("Timing")]
    public float beat;
    public float time = -1f;
    public bool useTime = false; // By default use beats

    // Item to instantiate
    [SerializeField] public Schedulable item;

    // Use another schedule instead
    [SerializeField] public Schedule subschedule;

    [Header("Transform")]
    // Position, rotation, and scale at which to spawn
    public Vector3 position = Vector3.zero;
    public Vector3 scale = Vector3.zero;
    public Quaternion rotation = Quaternion.identity;
    // Mostly unused but defaults to zero so use boolean flag
    public bool useScale = false;

    public Event CreateRuntimeCopy()
    {
      return (Event)MemberwiseClone();
    }

    public Event AtSubscheduledTime(float beat, float time, Vector3 position, Vector3 scale, Quaternion rotation)
    {
      Event copy = CreateRuntimeCopy();

      // Position is offset, others are multiplicative
      copy.position += position;
      copy.scale = Vector3.Scale(copy.scale, scale);
      copy.rotation *= rotation;

      // Beats start at 1, but offset of beat 1 should be zero
      copy.beat += beat - 1;

      // Only add time if not negative
      if (time >= 0) copy.time += time;

      return copy;
    }

    public float GetCanonTime(float bpm)
    {
      return time > 0 ? time : (float)MusicManager.Metronome.BeatsToTime(beat - 1);
    }
  }

  // What we set in the editor for events
  [SerializeField] private List<Event> _events = new();
  public IReadOnlyList<Event> rawEvents { get { return _events; } }

  // Tweak scale of positions
  public Vector3 axisScale = Vector3.one;

  // Version of events with subschedules replaced with their events
  public List<Event> events
  {
    get
    {
      List<Event> ret = _events
        .SelectMany(e => e.subschedule?.events?
          .Select(sub_e => sub_e.AtSubscheduledTime(e.beat, e.time, Vector3.Scale(e.position, axisScale), e.scale, e.rotation))
          ?? new List<Event>() { e })
        .ToList();
      return ret;
    }
  }
}
