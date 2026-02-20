using System.Collections.Generic;
using UnityEngine;


public class SyncAudioSources : MonoBehaviour
{
    private static SyncAudioSources _Instance;

    [SerializeField] private List<AudioSource> sources;

    void Awake()
    {
        _Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Could be uneccessary, ignore for now
        // _Sync();
    }

    void _Sync()
    {
        // Sync all audio sources to 0th
        for (int i = 1; i < sources.Count; i++)
        {
            sources[i].timeSamples = sources[0].timeSamples;
        }
    }

    void _Play(List<AudioClip> clips, List<float> volumes = null)
    {
        for (int i = 0; i < Mathf.Min(sources.Count, clips.Count); i++)
        {
            float volume = (volumes != null && i < volumes.Count) ? volumes[i] : 1.0f;

            sources[i].volume = volume;
            sources[i].clip = clips[i];
            sources[i].Play();
        }
    }

    void _PlayOne(AudioClip clip)
    {
        if (sources.Count < 1) return;

        sources[0].volume = 1.0f;
        sources[0].clip = clip;
        sources[0].Play();

        // Mute all other sources
        for (int i = 1; i < sources.Count; i++)
        {
            sources[i].Stop();
            sources[i].volume = 0f;
        }
    }

    void _SetVolumes(List<float> volumes)
    {
        for (int i = 0; i < Mathf.Min(sources.Count, volumes.Count); i++)
        {
            float volume = volumes[i];
            sources[i].volume = volume;
        }
    }

    public static void Play(List<AudioClip> clips, List<float> volumes = null)
    {
        _Instance._Play(clips, volumes);
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
