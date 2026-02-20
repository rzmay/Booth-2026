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
    public float time = -1;

    // Item to instantiate
    [SerializeField] public Schedulable item;

    // Transform to spawn relative to origin
    public Transform transform;
  }

  public List<Event> events = new();
}
