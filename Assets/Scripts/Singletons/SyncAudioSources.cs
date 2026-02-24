using System.Collections.Generic;
using UnityEngine;

public class SyncAudioSources : MonoBehaviour
{
    private static SyncAudioSources _Instance;

    [SerializeField] private List<AudioSource> sources;

    [SerializeField] private float volumeSmoothingSpeed = 5f;

    private float[] targetVolumes;

    void Awake()
    {
        _Instance = this;

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

    void _Play(List<AudioClip> clips = null, List<float> volumes = null, double dspTime = 0d)
    {
        if (clips != null) _SetTracks(clips);
        if (volumes != null) _SetVolumes(volumes, true);

        for (int i = 0; i < Mathf.Min(sources.Count, clips.Count); i++)
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

    void _PlayOne(AudioClip clip, double dspTime = 0)
    {
        if (sources.Count < 1) return;

        sources[0].clip = clip;
        sources[0].volume = 1.0f;
        targetVolumes[0] = 1.0f;
        sources[0].Play();
    }

    void _SetVolumes(List<float> volumes, bool setSourceVolume = false)
    {
        for (int i = 0; i < Mathf.Min(targetVolumes.Length, volumes.Count); i++)
        {
            targetVolumes[i] = volumes[i];
            if (setSourceVolume) sources[i].volume = volumes[i];
        }
    }

    void _SetTracks(List<AudioClip> tracks)
    {
        for (int i = 0; i < Mathf.Min(sources.Count, tracks.Count); i++)
        {
            sources[i].clip = tracks[i];
        }
    }

    void _Stop()
    {
        foreach (AudioSource source in sources)
        {
            source.Stop();
        }
    }

    public static void Play(List<AudioClip> clips = null, List<float> volumes = null, double dspTime = 0d)
    {
        _Instance._Play(clips, volumes, dspTime);
    }

    public static void PlayOne(AudioClip clip, double dspTime = 0d)
    {
        _Instance._PlayOne(clip, dspTime);
    }

    public static void SetVolumes(List<float> volumes, bool setSourceVolume = false)
    {
        _Instance._SetVolumes(volumes, setSourceVolume);
    }

    public static void SetTracks(List<AudioClip> clips)
    {
        _Instance._SetTracks(clips);
    }

    public static void Stop()
    {
        _Instance._Stop();
    }
}
