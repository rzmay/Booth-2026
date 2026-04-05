using System;
using UnityEngine;


// replace this class with the actual schedulable asset code. This is AI slop (and also doesnt have a menu)
public class SchedulableAsset : ScriptableObject
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