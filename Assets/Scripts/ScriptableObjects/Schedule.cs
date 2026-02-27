using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Schedule", menuName = "Scriptable Objects/Schedule")]
public class Schedule : ScriptableObject
{
  [System.Serializable]
  public class Event
  {
    // Use beats if time is negative
    public float beat;

    // By default, use beats
    public float time = -1f;

    // Item to instantiate
    [SerializeField] public Schedulable item;

    // Position, rotation, and scale at which to spawn
    public Vector3 position = Vector3.zero;
    public Vector3 scale = Vector3.one;
    public Quaternion rotation = Quaternion.identity;
  }

  public List<Event> events = new();
}
