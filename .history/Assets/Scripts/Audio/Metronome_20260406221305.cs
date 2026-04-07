using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(StemMixer))]
public sealed class Metronome : MonoBehaviour
{
    public float bpm = 120.0f;
    public float delay = 8f;

    [Header("Metronome Sound Settings")]
    public int countInBeats = 8;
    public float countInVolume = 1.0f;
    public float metronomeVolume = 0.0f;
    public float firstBeatPitch = 1f;
    public int beatsPerMeasure = 4;
    public float scheduleAheadBeats = 0.5f;

    // runtime state
    [HideInInspector] public bool isPlaying = false;
    private double startDspTime = 0.0;
    private int lastBeatIndex = 0;
    private int currBeatIndex = 0;
    private int scheduledBeatIndex = 0;

    // events
    // used for synthesizer, visualizer, or any other visual effects that need to be in sync with the beat of the song. This event is fired every single beat, and it provides the beat index and the exact DSP time of that beat.
    // good for metronome tick sound, visual pulse, haptic feedback, etc
    public event Action<int, double> OnBeat; // beatIndex, beatDspTime

    // used for the actual gameplay loop as the point scoring events (the punch circles / swipes and whatnot), fired every frame and does nothing if null, but if a function is subscribed to it with the proper float/double combo that corresponds to the current song time / beat index, then we can trigger scoring events
    // good for scoring player actions, triggering gameplay scoring events, all other events
    public event Action<float, double> OnMetronomeTime; // beatIndexFloat (in between beats), timeDspTime

    private AudioSource _metronomeAudioSource;
    private StemMixer _stems;
    private float _songCountInBeats = 0f; // How many beats before the first beat in the song?
    private int _safeCountInBeats
    {
        get
        {
            return Mathf.CeilToInt(
                    Mathf.Max(_songCountInBeats, countInBeats) / beatsPerMeasure
                ) * beatsPerMeasure;
        }
    }


    void Awake()
    {
        _metronomeAudioSource = GetComponent<AudioSource>();
        _stems = GetComponent<StemMixer>();
    }

    // updates the metronome state each frame
    void Update()
    {
        if (!isPlaying || bpm <= 0.0f)
            return;

        // time since metronome started
        double dspTime = AudioSettings.dspTime;
        double songTime = GetSongTimeSeconds(dspTime);
        float beatFloat = TimeToBeats(songTime) + 1;

        OnMetronomeTime?.Invoke(beatFloat, songTime);

        currBeatIndex = Mathf.FloorToInt(beatFloat);


        // If we haven't scheduled a click for the next beat, schedule it now
        int nextBeat = currBeatIndex + 1;
        if (scheduledBeatIndex < nextBeat)
        {
            float beatTimeToNext = 1 - (beatFloat - currBeatIndex);

            // Only schedule if the metronome is not currently playing or if we have passed the scheduling threshhold
            bool shouldSchedule = !_metronomeAudioSource.isPlaying || beatTimeToNext <= scheduleAheadBeats;

            if (shouldSchedule) ScheduleBeatClick(nextBeat, dspTime + BeatsToTime(beatTimeToNext));
        }

        // Catch-up loop: if we skipped beats from low FPS, fire all events
        // in order. If no skipped beats, only fires the current beat.
        for (int beat = lastBeatIndex + 1; beat <= currBeatIndex; beat++)
        {
            // calcs exact time of this beat
            double beatDspTime = startDspTime + BeatsToTime(beat);

            // invoke on-beat event
            // this is an event that happens every single beat. We don't need to have an event here (we could just have it be null and its fine) but this on-beat event can be used for a synthesizer, visualizer, or any other visual effects that need to be in sync with the beat of the song.
            OnBeat?.Invoke(beat, beatDspTime);
            lastBeatIndex = beat;
        }
    }

    private void ScheduleBeatClick(int index, double dspTime)
    {
        // If the current beat index is less than zero, use count in volume
        float volume = index > 0 ? metronomeVolume : countInVolume;

        // Change pitch if this is the first beat of the phrase
        int beatOfMeasure = Mathf.Abs(index) % beatsPerMeasure;
        if (index < 0) beatOfMeasure = beatsPerMeasure - beatOfMeasure;

        float pitch = 1.0f + (beatOfMeasure == 1 ? firstBeatPitch : 0);

        _metronomeAudioSource.pitch = pitch;
        _metronomeAudioSource.volume = volume;
        _metronomeAudioSource.PlayScheduled(dspTime);

        // Track scheduling
        scheduledBeatIndex = index;
    }

    // Gets the initial DSP time and starts the music. Changes isPlaying to true.
    public void Play(SongData songData = null)
    {
        if (isPlaying) return;

        if (songData != null) LoadSongData(songData);

        startDspTime = AudioSettings.dspTime + delay + BeatsToTime(_safeCountInBeats);
        isPlaying = true;
        lastBeatIndex = -_safeCountInBeats;
        scheduledBeatIndex = lastBeatIndex;

        // First stop the music
        _stems.Stop();

        // Play music -- substract songCountIn to start at first beat
        _stems.Play(null, null, startDspTime - BeatsToTime(_songCountInBeats));
    }

    public void LoadSongData(SongData songData)
    {
        bpm = songData.bpm;
        beatsPerMeasure = songData.beatsPerMeasure;

        _songCountInBeats = songData.countInBeats;
    }

    // stops the metronome
    public void Stop()
    {
        isPlaying = false;
        _stems.Stop();
        startDspTime = 0.0;
        lastBeatIndex = 0;
        scheduledBeatIndex = lastBeatIndex;
    }

    //####### HELPER FUNCTIONS / UTILITY FUNCTIONS #######//

    // returns the current beat phase (0.0 to 1.0) of the metronome
    // 0.0 = start of beat, 0.5 = middle of beat, 1.0 = end of beat, etc
    // can be used for triplets, quartets, etc
    public float GetBeatPhase()
    {
        if (!isPlaying)
            return 0.0f;

        if (bpm <= 0.0f)
            return 0.0f;

        double secsPerBeat = 60.0 / bpm;
        double songTime = GetSongTimeSeconds(AudioSettings.dspTime);
        double beatTime = songTime;

        if (beatTime < 0.0)
            return 0.0f;

        double beatPos = beatTime / secsPerBeat;
        return (float)(beatPos - Math.Floor(beatPos));
    }

    public float GetBeatFloat()
    {
        if (!isPlaying || bpm == 0.0f)
            return 0.0f;

        // Beats start counting at 1
        return TimeToBeats(GetSongTimeSeconds(AudioSettings.dspTime)) + 1;
    }

    // public function to get how many seconds have passed since the metronome started playing
    public double GetSongTimeSeconds(double dspTime)
    {
        if (!isPlaying)
            return 0.0;

        double songTime = dspTime - startDspTime;
        return songTime;
    }

    public float TimeToBeats(double time)
    {
        return (float)time * bpm / 60f;
    }

    public double BeatsToTime(float beats)
    {
        return (double)(beats * 60f / bpm);
    }
}
