using System.Collections;
using UnityEngine;


// METRONOME CLASS
// Handles the metronome functionality within the application including timing, beat, etc
public sealed class Metronome : MonoBehaviour
{
    [SerializeField] float bpm = 120.0f;
    [SerializeField] float offset = 0.0f;
    float delay = 0.1f;
    bool isPlaying = false;
    float dspTimeAtStart = 0.0f;

    // DEFINE AUDIO SOURCE UNDER
    [SerializeField] AudioSource src_audio;

    public void Play()
    {
        float curDSP = AudioSettings.dspTime;
        float firstTick = curDSP + offset + delay;
        dspTimeAtStart = firstTick;
        isPlaying = true;

        audio_src.Play();
    }
}
