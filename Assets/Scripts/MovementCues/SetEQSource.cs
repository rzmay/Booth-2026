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
            if (SyncAudioSources.Instance.sources.Count < i)
            {
                _ringEQ.sources.Add(SyncAudioSources.Instance.sources[i]);
            }
        }
    }
}
