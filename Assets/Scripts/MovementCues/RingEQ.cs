using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CircleLineRenderer))]
public class RingEQ : MonoBehaviour
{
    public SpectrumData spectrum;

    [Header("Visualization")]
    public float amplitude = 0.75f;
    public float power = 1.25f;
    public bool mirror = false;

    [Header("Wave")]
    [Tooltip("Whether or not to use a wave")]
    public bool useWave = true;
    [Tooltip("Frequency of the wave")]
    public int waveFrequency = 12;
    [Tooltip("How many beats to switch wave phase on (0 = off)")]
    public float phaseFrequency = 1.0f;
    [Tooltip("How fast to smooth phase changes")]
    public float phaseSmoothSpeed = 25f;
    [Tooltip("Scale wave frequency with amplitude?")]
    public bool scaleFrequencyWithAmplitude = false;
    [Tooltip("Scale wave frequency with spectrum frequency")]
    public bool scaleFrequencyWithSpectrum = true;
    public float scaleFrequencyWithSpectrumAmount = 2.0f;
    [Tooltip("Change phase on transients?")]
    public bool useTransients = true;
    [Tooltip("Threshhold delta for transient detection")]
    public float transientThreshhold = 1.50f;
    private CircleLineRenderer _circle;
    private int _phase = 1;     // 1 or -1
    private float _phaseSmooth = 0;
    private float _lastPhaseFrequency = 0;
    private float _lastTransientThreshhold = 0;

    void Awake()
    {
        _circle = GetComponent<CircleLineRenderer>();
    }

    void Start()
    {
        _circle.SetOffsetFunction(GetOffset);
    }

    void OnEnable()
    {
        SetupSpectrum();
    }

    // unsubscribe to metronome events
    void OnDisable()
    {
        if (!spectrum) return;

        spectrum.OnBeat[phaseFrequency] -= OnBeat;
        spectrum.OnTransient[transientThreshhold] -= OnTransient;
    }

    void Update()
    {
        _phaseSmooth = Mathf.Lerp(_phaseSmooth, _phase, phaseSmoothSpeed * Time.deltaTime);

        if (!spectrum) SetupSpectrum();

        // Allow dynamic changes
        if (_lastPhaseFrequency != phaseFrequency || _lastTransientThreshhold != transientThreshhold)
        {
            // Deregister
            spectrum.OnBeat[_lastPhaseFrequency] -= OnBeat;
            spectrum.OnTransient[_lastTransientThreshhold] -= OnTransient;

            // Register new
            spectrum.OnBeat[phaseFrequency] += OnBeat;
            spectrum.OnTransient[transientThreshhold] += OnTransient;

            _lastPhaseFrequency = phaseFrequency;
            _lastTransientThreshhold = transientThreshhold;
        }
    }

    void SetupSpectrum()
    {
        if (!spectrum) return;

        spectrum.numBands = _circle.resolution;

        spectrum.OnBeat[phaseFrequency] += OnBeat;
        spectrum.OnTransient[transientThreshhold] += OnTransient;
    }

    public float GetOffset(float t)
    {
        if (!spectrum) return 0.0f;

        float m;

        if (mirror)
        {
            // Seam-mirrored mapping:
            // t=0 is top on one side and bottom on the other (your working version)
            float s = Mathf.Repeat(t, 1f);
            float tri = 1f - Mathf.Abs(2f * s - 1f);
            m = (s <= 0.5f) ? tri : (1f - tri);
            m = Mathf.Repeat(m + 0.5f, 1f);
        }
        else
        {
            // Original mapping:
            // top (t=0.25) -> m=0, bottom -> m=1, symmetric left/right
            float u = Mathf.Repeat(t - 0.25f, 1f);
            m = 1f - Mathf.Abs(2f * u - 1f);
        }

        int index = Mathf.Clamp(
            Mathf.FloorToInt(m * (spectrum.bands.Length - 1)),
            0,
            spectrum.bands.Length - 1
        );

        float value = spectrum.bands[index];

        value = Mathf.Pow(value, power);

        float waveVal = 1f;
        if (useWave)
        {
            float phaseMultiplier = 1f;

            if (scaleFrequencyWithAmplitude) phaseMultiplier *= value;
            if (scaleFrequencyWithSpectrum) phaseMultiplier *= index * scaleFrequencyWithSpectrumAmount / (spectrum.bands.Length - 1);

            waveVal = Mathf.Sin(m * 2 * Mathf.PI * waveFrequency * phaseMultiplier) * _phaseSmooth;
        }

        return value * amplitude * waveVal;
    }

    void OnBeat(float _, double __)
    {
        SwitchPhase();
    }

    void OnTransient(float _)
    {
        if (useTransients) SwitchPhase();
    }

    void SwitchPhase()
    {
        _phase = -_phase;
    }
}
