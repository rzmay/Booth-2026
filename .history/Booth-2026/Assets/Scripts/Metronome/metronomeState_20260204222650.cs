using System;
using UnityEngine;

// MetronomeState class
public sealed class MetronomeState
{
    private bool isPlaying;
    private double startDspTime;
    private float bpm;
    private float offsetSeconds;
    private double secondsPerBeat;
    private int lastBeatIndex;

    //#### START AND STOP FUNCTIONS ####//
    public void Start(double startDspTime, float bpm, float offset)
    {
        this.isPlaying = true;
        this.startDspTime = startDspTime;
        this.bpm = bpm;
        this.offsetSeconds = offset;
        this.secondsPerBeat = 60.0 / bpm;
        this.lastBeatIndex = -1;
    }

    public void Stop()
    {
        this.isPlaying = false;
        this.lastBeatIndex = -1;
        this.startDspTime = 0.0;
    }

    //#### TIME HELPERS ####//

    // gets current song time in seconds
    public double getSongTimeSeconds(double dspNow)
    {
        if (!isPlaying)
            return 0.0;

        return dspNow - startDspTime;
    }

    // gets current beat time in seconds (offset of song time)
    public double getBeatTimeSeconds(double dspNow)
    {
        if (!isPlaying || bpm <= 0.0f)
            return 0.0;

        double songTime = getSongTimeSeconds(dspNow);
        return songTime - offsetSeconds;
    }

    // gets current beat index (since start)
    public int getBeatIndex(double dspNow)
    {
        if (!isPlaying || bpm <= 0.0f)
            return -1;

        double beatTime = getBeatTimeSeconds(dspNow);
        if (beatTime < 0.0)
            return -1;

        return (int)(beatTime / secondsPerBeat);
    }

    // computes the exact dsp time at which a given beat boundary occurs
    public double getDspTimeForBeat(int beatIndex)
    {
        return startDspTime + offsetSeconds + (beatIndex * secondsPerBeat);
    }

    //#### EVENT DISPATCH ####//
    public bool DispatchBeatEvents(double dspNow, Action<int, double> onBeat)
    {
        if (!isPlaying || bpm <= 0.0f || onBeat == null)
            return false;

        int currentBeatIndex = getBeatIndex(dspNow);
        if (currentBeatIndex < 0)
            return false;

        // simple loop to dispatch all missed beat events. Same as metronome.cs
        for (int beat = lastBeatIndex + 1; beat <= currentBeatIndex; beat++)
        {
            double beatDspTime = getDspTimeForBeat(beat);
            onBeat(beat, beatDspTime);
            lastBeatIndex = beat;
        }
        return (int)(beatTime / secondsPerBeat);
    }
}