using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HapticsController))]
public class Player : MonoBehaviour
{
    public static Player Instance;

    public Hand leftHand;
    public Hand rightHand;
    public AudioSource obstacleAudioSource;
    [HideInInspector] public HapticsController hapticsController;

    void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hapticsController = GetComponent<HapticsController>();
    }
}
