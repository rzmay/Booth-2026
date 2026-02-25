using System.Collections.Generic;
using UnityEngine;

public class StemMixer : MonoBehaviour
{
    [SerializeField] private List<AudioSource> sources;

    [SerializeField] private float volumeSmoothingSpeed = 5f;

    private float[] targetVolumes;

    void Awake()
    {
        // Initialize target volumes to current source volumes
        targetVolumes = new float[sources.Count];
    }

    void Update()
    {
        SmoothVolumes();
    }

    void SmoothVolumes()
    {
        // Maintain size parity
        if (sources.Count != targetVolumes.Length) System.Array.Resize<float>(ref targetVolumes, sources.Count);

        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i] == null) continue;

            float current = sources[i].volume;
            float target = targetVolumes[i];

            sources[i].volume = Mathf.Lerp(
                current,
                target,
                volumeSmoothingSpeed * Time.deltaTime
            );
        }
    }

    void _Sync()
    {
        for (int i = 1; i < sources.Count; i++)
        {
            sources[i].timeSamples = sources[0].timeSamples;
        }
    }

    public void Play(List<AudioClip> clips = null, List<float> volumes = null, double dspTime = 0d)
    {
        if (clips != null) SetTracks(clips);
        if (volumes != null) SetVolumes(volumes, true);

        for (int i = 0; i < sources.Count; i++)
        {
            if (dspTime <= 0d)
            {
                sources[i].Play();
            }
            else
            {
                sources[i].PlayScheduled(dspTime);
            }
        }
    }

    public void PlayOne(AudioClip clip, double dspTime = 0)
    {
        if (sources.Count < 1) return;

        sources[0].clip = clip;
        sources[0].volume = 1.0f;
        targetVolumes[0] = 1.0f;
        sources[0].Play();
    }

    public void SetVolumes(List<float> volumes, bool setSourceVolume = false)
    {
        for (int i = 0; i < Mathf.Min(targetVolumes.Length, volumes.Count); i++)
        {
            targetVolumes[i] = volumes[i];
            if (setSourceVolume) sources[i].volume = volumes[i];
        }
    }

    public void SetTracks(List<AudioClip> tracks)
    {
        for (int i = 0; i < Mathf.Min(sources.Count, tracks.Count); i++)
        {
            sources[i].clip = tracks[i];
        }
    }

    public void SetLoopTime(double loopTime)
    {
        foreach (AudioSource source in sources)
        {
            LoopFromTime looper = source.GetComponent<LoopFromTime>();
            if (looper != null) looper.loopStartTime = loopTime;
        }
    }

    public void Stop()
    {
        foreach (AudioSource source in sources)
        {
            source.Stop();

            LoopFromTime looper = source.GetComponent<LoopFromTime>();
            if (looper != null) looper.Stop();
        }
    }
}
