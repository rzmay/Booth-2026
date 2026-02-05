using UnityEngine;
using System;


// METRONOME CLASS
// Handles the metronome functionality within the application
// Stard
public sealed class Metronome : MonoBehaviour
{
    // VARIABLE INITIALIZATION //
    [SerializeField] private float bpm = 120.0f;
    [SerializeField] private float offset = 0.0f;
    [SerializeField] private float delay = 0.1f;
    private bool isPlaying = false;
    private double startDspTime = 0.0;

    // DEFINE AUDIO SOURCE UNDER //
    [SerializeField] AudioSource musicSource;

    public event Action<int, double> OnBeat; // beatIndex, beatDspTime

    int lastBeatIndex = -1;


    //####### METRONOME CORE METHODS #######//

    // Gets the initial DSP time and starts the music. Changes isPlaying to true.
    public void Play()
    {
        startDspTime = AudioSettings.dspTime + delay;
        isPlaying = true;
        lastBeatIndex = -1;

        // play the music (if exists)
        musicSource?.PlayScheduled(startDspTime);
    }

    // updates the metronome state each frame
    private void Update()
    {
        if (!isPlaying)
            return;

        //SAFETY: 0 bpm is invalid
        if (bpm <= 0.0f)
            return;

        // compute current beat index
        double secsPerBeat = 60.0 / bpm;

        // time since metronome started, then adjusted for offset
        double songTime = getSongTimeSeconds();
        double beatTime = songTime - offset;

        if (beatTime < 0.0)
            return; // not yet reached offset

        int curBeatIndex = (int)Math.Floor(beatTime / secsPerBeat);


        // Catch-up loop: if we skipped beats from low FPS, fire all events
        // in order. If no skipped beats, only fires the current beat.
        for (int beat = lastBeatIndex + 1; beat <= curBeatIndex; beat++)
        {
            // calcs exact time of this beat
            double beatDspTime = startDspTime + offset + beat * secsPerBeat;

            // invoke on-beat event
            OnBeat?.Invoke(beat, beatDspTime);
            lastBeatIndex = beat;
        }
    }

    // stops the metronome
    public void Stop()
    {
        isPlaying = false;
        musicSource?.Stop();
        startDspTime = 0.0;
        lastBeatIndex = -1;
    }

    //####### HELPER FUNCTIONS / UTILITY FUNCTIONS #######//

    // private helper that returns current dsp time
    private double getDSPTime()
    {
        return AudioSettings.dspTime;
    }

    // grabs the current BPM
    public float getBPM()
    {
        return bpm;
    }

    // public function to get how many seconds have passed since the metronome started playing
    public double getSongTimeSeconds()
    {
        if (!isPlaying)
            return 0.0;

        double dspTime = getDSPTime();
        double songTime = dspTime - startDspTime;
        return songTime;
    }
}
