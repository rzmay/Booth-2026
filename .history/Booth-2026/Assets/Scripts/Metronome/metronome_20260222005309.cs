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
    [SerializeField] private SyncAudioSources musicSource;

    // runtime state
    private bool isPlaying = false;
    private double startDspTime = 0.0;
    private int lastBeatIndex = -1;
    private int curBeatIndex = -1;

    // events
    // used for synthesizer, visualizer, or any other visual effects that need to be in sync with the beat of the song. This event is fired every single beat, and it provides the beat index and the exact DSP time of that beat.
    public event Action<int, double> VisualizerOnBeat; // beatIndex, beatDspTime

    // used for the actual gameplay loop as the point scoring events (the punch circles / swipes and whatnot), fired every frame and does nothing if null, but if a function is subscribed to it with the proper float/double combo that corresponds to the current song time / beat index, then we can trigger scoring events
    public event Action<float, double> ScoringEvent; // beatIndexFloat (in between beats), timeDspTime


    //####### METRONOME CORE CONTROLS #######//

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

        float beatFloat = (float)(beatTime / secsPerBeat);
        ScoringEvent?.Invoke(beatFloat, songTime);

        curBeatIndex = (int)Math.Floor(beatTime / secsPerBeat);


        // Catch-up loop: if we skipped beats from low FPS, fire all events
        // in order. If no skipped beats, only fires the current beat.
        for (int beat = lastBeatIndex + 1; beat <= curBeatIndex; beat++)
        {
            // calcs exact time of this beat
            double beatDspTime = startDspTime + offset + beat * secsPerBeat;

            // invoke on-beat event
            // this is an event that happens every single beat. We don't need to have an event here (we could just have it be null and its fine) but this on-beat event can be used for a synthesizer, visualizer, or any other visual effects that need to be in sync with the beat of the song.
            VisualizerOnBeat?.Invoke(beat, beatDspTime);
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

    // returns the current beat phase (0.0 to 1.0) of the metronome
    // 0.0 = start of beat, 0.5 = middle of beat, 1.0 = end of beat, etc
    // can be used for triplets, quartets, etc
    public float getBeatPhase()
    {
        if (!isPlaying)
            return 0.0f;

        if (bpm <= 0.0f)
            return 0.0f;

        double secsPerBeat = 60.0 / bpm;
        double songTime = getSongTimeSeconds();
        double beatTime = songTime - offset;

        if (beatTime < 0.0)
            return 0.0f;

        double beatPos = beatTime / secsPerBeat;
        return (float)(beatPos - Math.Floor(beatPos));
    }

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

    // gets beatIndex based on current song time
    public int getCurrentBeatIndex()
    {
        return curBeatIndex;
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }
}
