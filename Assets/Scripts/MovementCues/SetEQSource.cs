using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RingEQ))]
public class SetEQSource : MonoBehaviour
{
    [SerializeField] public List<int> sources = new();
    private RingEQ _ringEQ;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _ringEQ = GetComponent<RingEQ>();

        foreach (int i in sources)
        {
            AudioSource source = MusicManager.Stems.sources[i];
            _ringEQ.sources.Add(source);

            LoopFromTime looper = source.GetComponent<LoopFromTime>();
            if (looper != null) _ringEQ.sources.Add(looper.sourceB);
        }
    }
}
