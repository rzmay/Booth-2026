using System.Collections.Generic;
using UnityEngine;

public class SyncAudioSources : MonoBehaviour
{
    private static SyncAudioSources _Instance;

    [SerializeField] private List<AudioSource> sources;

    [SerializeField] private float volumeSmoothingSpeed = 5f;

    private List<float> targetVolumes = new List<float>();

    void Awake()
    {
        _Instance = this;

        // Initialize target volumes to current source volumes
        targetVolumes.Clear();
        for (int i = 0; i < sources.Count; i++)
        {
            targetVolumes.Add(sources[i] != null ? sources[i].volume : 0f);
        }
    }

    void Update()
    {
        SmoothVolumes();
    }

    void SmoothVolumes()
    {
        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i] == null) continue;

            // Ensure target list stays in sync
            if (i >= targetVolumes.Count)
                targetVolumes.Add(sources[i].volume);

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

    void _Play(List<AudioClip> clips, List<float> volumes = null, double dspTime = 0d)
    {
        for (int i = 0; i < Mathf.Min(sources.Count, clips.Count); i++)
        {
            float volume = (volumes != null && i < volumes.Count) ? volumes[i] : 1.0f;

            sources[i].clip = clips[i];
            sources[i].volume = volume;

            if (dspTime == 0d)
            {
                sources[i].Play();
            }
            else
            {
                sources[i].PlayScheduled(dspTime);
            }

            // Set both current and target
            if (i < targetVolumes.Count)
                targetVolumes[i] = volume;
        }
    }

    void _PlayOne(AudioClip clip)
    {
        if (sources.Count < 1) return;

        sources[0].clip = clip;
        sources[0].volume = 1.0f;
        sources[0].Play();

        if (targetVolumes.Count > 0)
            targetVolumes[0] = 1.0f;

        for (int i = 1; i < sources.Count; i++)
        {
            sources[i].Stop();
            sources[i].volume = 0f;

            if (i < targetVolumes.Count)
                targetVolumes[i] = 0f;
        }
    }

    void _SetVolumes(List<float> volumes)
    {
        for (int i = 0; i < Mathf.Min(sources.Count, volumes.Count); i++)
        {
            if (i < targetVolumes.Count)
                targetVolumes[i] = volumes[i];
            else
                targetVolumes.Add(volumes[i]);
        }
    }

    public static void Play(List<AudioClip> clips, List<float> volumes = null, double dspTime = 0d)
    {
        _Instance._Play(clips, volumes, dspTime);
    }

    public static void PlayOne(AudioClip clip)
    {
        _Instance._PlayOne(clip);
    }

    public static void SetVolumes(List<float> volumes)
    {
        _Instance._SetVolumes(volumes);
    }
}
