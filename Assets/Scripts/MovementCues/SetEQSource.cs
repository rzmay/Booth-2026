using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RingEQ))]
public class SetEQSource : MonoBehaviour
{
    [SerializeField] public List<int> sources = new();
    private SpectrumData _spectrum;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _spectrum = GetComponent<SpectrumData>();

        foreach (int i in sources)
        {
            AudioSource source = MusicManager.Stems.sources[i];
            _spectrum.sources.Add(source);

            LoopFromTime looper = source.GetComponent<LoopFromTime>();
            if (looper != null) _spectrum.sources.Add(looper.sourceB);
        }
    }
}
