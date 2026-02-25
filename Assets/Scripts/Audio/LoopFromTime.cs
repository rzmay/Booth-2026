using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LoopFromTime : MonoBehaviour
{
    [SerializeField] public AudioSource sourceB;
    [Min(0f)]
    [SerializeField] public double loopStartTime = 0f;

    int _loopStartSamples
    {
        get
        {
            return Mathf.Clamp(
                (int)Math.Round(loopStartTime * sourceA.clip.frequency),
                0,
                sourceA.clip.samples - 1
            );
        }
    }
    double _loopDur
    {
        get
        {
            return sourceA.clip.length - loopStartTime;
        }
    }

    AudioSource sourceA;     // Set from Start

    AudioSource _lastActive;
    double _nextDspStart;    // when _next starts
    double _nextDspEnd;      // when _next ends
    double _prevDsp;

    void Awake()
    {
        sourceA = GetComponent<AudioSource>();
    }
    void Start()
    {
        if (sourceA) sourceA.loop = false;
        if (sourceB) sourceB.loop = false;

        _prevDsp = AudioSettings.dspTime;
        _nextDspStart = -1;
        _nextDspEnd = 0;
    }

    void Update()
    {
        // Sync volume to sourceA -- no other params are changed dynamically
        sourceB.volume = sourceA.volume;

        // If neither is playing we can exit -- no need to schedule
        if (!sourceA.isPlaying && !sourceB.isPlaying) return;

        // Whenever we enter the scheduled loop, schedule the next
        double dsp = AudioSettings.dspTime;
        if ((_prevDsp <= _nextDspStart || _nextDspStart < 0) && _nextDspStart < dsp) ScheduleLoop();
        _prevDsp = dsp;
    }

    void ScheduleLoop()
    {
        Debug.Log("Scheduling Loop");

        // Active is either currently detected as playing, or next from the previously active source
        AudioSource active = _lastActive == null ? GetActive() : GetNextFromActive(_lastActive);

        // Don't schedule until audio actually starts
        if (active.timeSamples <= 0) return;

        // Next can easily be calculated from active either way
        AudioSource next = GetNextFromActive(active);

        // Compute exact DSP time when active ends (end of clip).
        Debug.Log($"Active time: {active.time}");
        double activeEndsAt = _nextDspEnd > 0 ? _nextDspEnd : AudioSettings.dspTime + (active.clip.length - active.time);
        active.SetScheduledEndTime(activeEndsAt);

        // Schedule the first loop segment on _next: [loopStartTime -> clip end]
        next.clip = active.clip;
        next.loop = false;
        next.timeSamples = _loopStartSamples;

        next.PlayScheduled(activeEndsAt);
        next.SetScheduledEndTime(activeEndsAt + _loopDur);

        // Now set up state for chaining: after _next ends, the following segment will be on active.
        _nextDspStart = activeEndsAt;
        _nextDspEnd = activeEndsAt + _loopDur;

        // Set last active as well for stability in next loop
        _lastActive = active;
    }

    public AudioSource GetActive()
    {
        return sourceA.isPlaying && !sourceB.isPlaying ? sourceA
              : sourceB.isPlaying && !sourceA.isPlaying ? sourceB
              : sourceA; // Default to sourceA
    }

    public AudioSource GetNext()
    {
        return GetNextFromActive(GetActive());
    }

    AudioSource GetNextFromActive(AudioSource active)
    {
        return active == sourceA ? sourceB : sourceA;
    }

    public void Stop()
    {
        // Stop both sources
        sourceA.Stop();
        sourceB.Stop();

        // Reset internal state
        _lastActive = null;
        _prevDsp = AudioSettings.dspTime;
        _nextDspStart = -1;
        _nextDspEnd = 0;
    }
}
