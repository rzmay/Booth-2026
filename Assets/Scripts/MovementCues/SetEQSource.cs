using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RingEQ))]
public class SetEQSource : MonoBehaviour
{
    [SerializeField] public List<int> sources = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RingEQ ring = GetComponent<RingEQ>();

        foreach (int i in sources)
        {
            AudioSource source = MusicManager.Stems.sources[i];
            SpectrumData spectrum = source.GetComponent<SpectrumData>();

            ring.spectrum = spectrum;
        }
    }
}
