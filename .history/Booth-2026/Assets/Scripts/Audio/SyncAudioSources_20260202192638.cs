using System.Collections.Generic;
using UnityEngine;


public class SyncAudioSources : MonoBehaviour
{
    [SerializeField] private List<AudioSource> sources;

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

    public void Play(List<AudioClip> clips, List<float> volumes = null)
    {
        for (int i = 0; i < Mathf.Min(sources.Count, clips.Count); i++)
        {
            float volume = (volumes != null && i < volumes.Count) ? volumes[i] : 1.0f;

            sources[i].volume = volume;
            sources[i].clip = clips[i];
            sources[i].Play();
        }
    }

    public void PlayOne(AudioClip clip)
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
}
