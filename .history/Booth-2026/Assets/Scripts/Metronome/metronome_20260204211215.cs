using UnityEngine;


// METRONOME CLASS
// Handles the metronome functionality within the application including timing, beat, etc
public sealed class Metronome : MonoBehaviour
{
    [SerializeField] float bpm = 120.0f;
    [SerializeField] float offset = 0.0f;
    float delay = 0.1f;
    bool isPlaying = false;
    double dspTimeAtStart = 0.0f;

    // DEFINE AUDIO SOURCE UNDER
    [SerializeField] AudioSource src_audio;

    public void Play()
    {
        dspTimeAtStart = AudioSettings.dspTime + delay;
        isPlaying = true;

        src_audio.Play();
    }
}
