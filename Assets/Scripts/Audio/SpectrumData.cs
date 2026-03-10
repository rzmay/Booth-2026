using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpectrumData : MonoBehaviour
{
    public enum TransientHeuristic
    {
        Flux,
        Max,
        Avg
    }

    [Header("Audio Inputs")]
    public List<AudioSource> sources = new List<AudioSource>();
    public bool useListener = false;

    [Header("Spectrum Settings")]
    public int numBands = 64;
    public int fftSize = 1024;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    public bool useLogScaling = true;

    public float startFrequencyRange = 0.0f;
    public float endFrequencyRange = 1.0f;

    [Header("Smoothing")]
    [Tooltip("Whether or not to use smoothing")]
    public bool useSmoothing = true;

    [Tooltip("How fast the visual responds when increasing")]
    public float attackSpeed = 50f;

    [Tooltip("How fast the visual falls when decreasing")]
    public float releaseSpeed = 10f;

    [Header("Level Mapping")]
    public bool useDbMapping = true;
    public float dbMin = -80f;
    public float dbMax = -20f;

    [Header("Transient Detection")]
    public bool detectTransients = true;
    public TransientHeuristic detectionHeuristic = TransientHeuristic.Flux;
    [Tooltip("Activate when the detection heuristic is above a threshhold rather than comparing to buffer")]
    public bool useFluxThreshhold = false;
    public int fluxWindowSamples = 30;
    [Tooltip("How many samples to skip recording. Higher values record more time at lower resolution.")]
    public int skipSamples = 0;
    public float cooldownSeconds = 0.1f;


    public EventMap<float, Action<float, double>> OnBeat = new(); // Register by fraction of beat
    public EventMap<float, Action<float>> OnTransient = new(); // Resgister by detection threshhold

    private Metronome _metronome;
    private Dictionary<float, int> _lastBeat = new();
    private Dictionary<float, float> _lastTransientTime = new();
    private float[] _fluxBuffer;
    private int _sample = 0;


    private float[] _spectrumBands;
    private float[] _prevBands;
    private float[] _smoothedBands;
    public float[] bands { get { return useSmoothing ? _smoothedBands : _spectrumBands; } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _metronome = MusicManager.Metronome;

        fftSize = Mathf.NextPowerOfTwo(Mathf.Max(64, numBands, fftSize));
        _spectrumBands = new float[numBands];
        _prevBands = new float[numBands];
        _smoothedBands = new float[numBands];
        _fluxBuffer = new float[fluxWindowSamples];

        _metronome.OnMetronomeTime += OnMetronomeTime;

        AudioSource source = GetComponent<AudioSource>();
        if (source) sources.Add(source);

        // Include sourceB if looper present
        LoopFromTime looper = source.GetComponent<LoopFromTime>();
        if (looper && looper.sourceB) sources.Add(looper.sourceB);
    }

    // Update is called once per frame
    void Update()
    {
        // Update fft size
        fftSize = Mathf.NextPowerOfTwo(Mathf.Max(64, numBands, fftSize));

        // Update band count if changed
        if (_spectrumBands.Length != numBands || _prevBands.Length != numBands || _smoothedBands.Length != numBands)
        {
            System.Array.Resize(ref _spectrumBands, numBands);
            System.Array.Resize(ref _prevBands, numBands);
            System.Array.Resize(ref _smoothedBands, numBands);
        }

        if (_fluxBuffer.Length != fluxWindowSamples)
        {
            System.Array.Resize(ref _fluxBuffer, fluxWindowSamples);
        }

        UpdateSpectrum();
        DetectTransients();
    }

    public void UpdateSpectrum()
    {
        float[] spectrumRaw = new float[fftSize];

        if (useListener)
        {
            AudioListener.GetSpectrumData(spectrumRaw, 0, fftWindow);
        }
        else
        {
            int activeSources = 0;

            foreach (var src in sources)
            {
                if (src == null || !src.isPlaying) continue;

                float[] temp = new float[fftSize];
                src.GetSpectrumData(temp, 0, fftWindow);

                for (int i = 0; i < fftSize; i++)
                    spectrumRaw[i] += temp[i];

                activeSources++;
            }

            if (activeSources > 0)
            {
                for (int i = 0; i < fftSize; i++)
                    spectrumRaw[i] /= activeSources;
            }
        }

        if (startFrequencyRange > 0.0f || endFrequencyRange < 1.0f && startFrequencyRange < endFrequencyRange)
        {
            int spectrumStart = Mathf.CeilToInt(spectrumRaw.Length * startFrequencyRange);
            int spectrumEnd = Mathf.CeilToInt(spectrumRaw.Length * endFrequencyRange);

            float[] spectrumRestricted = spectrumRaw[spectrumStart..spectrumEnd];
            spectrumRaw = ArrayUtil.Resample(spectrumRestricted, spectrumRaw.Length);
        }

        BuildBands(spectrumRaw);
    }

    private void BuildBands(float[] spectrumRaw)
    {
        // Save old bands
        Array.Copy(bands, _prevBands, bands.Length);

        int n = spectrumRaw.Length;

        // Precompute centers (bin indices)
        int[] centers = new int[numBands];
        int stepLinear = Mathf.Max(1, n / numBands);

        for (int i = 0; i < numBands; i++)
        {
            if (!useLogScaling)
            {
                centers[i] = Mathf.Clamp(i * stepLinear + stepLinear / 2, 0, n - 1);
            }
            else
            {
                float t = (numBands == 1) ? 0f : (float)i / (numBands - 1);
                float logIndex = Mathf.Pow(t, 2f) * (n - 1);
                centers[i] = Mathf.Clamp(Mathf.RoundToInt(logIndex), 0, n - 1);
            }
        }

        // Ensure centers are non-decreasing (can happen with rounding in log mode)
        for (int i = 1; i < numBands; i++)
            centers[i] = Mathf.Max(centers[i], centers[i - 1]);

        for (int i = 0; i < numBands; i++)
        {
            int start, end;

            if (!useLogScaling)
            {
                // Simple linear fixed-width window
                int half = stepLinear / 2;
                start = Mathf.Max(0, centers[i] - half);
                end = Mathf.Min(n, centers[i] + half + 1);
            }
            else
            {
                // Log mode: edges from midpoints between centers
                int leftMid = (i == 0) ? 0 : (centers[i - 1] + centers[i]) / 2;
                int rightMid = (i == numBands - 1) ? n : (centers[i] + centers[i + 1]) / 2;

                start = Mathf.Clamp(leftMid, 0, n - 1);
                end = Mathf.Clamp(rightMid, start + 1, n); // end exclusive, at least 1 bin
            }

            float sum = 0f;
            for (int j = start; j < end; j++) sum += spectrumRaw[j];

            float value = sum / (end - start);
            // float value = sum;

            if (useDbMapping)
            {
                float db = 20f * Mathf.Log10(value + 1e-7f);
                value = Mathf.Clamp01(Mathf.InverseLerp(dbMin, dbMax, db));
            }

            _spectrumBands[i] = value;
            SetSmoothedBands(i);
        }
    }

    // Do this element-wise rather than using a second loop for more speed
    void SetSmoothedBands(int i)
    {
        float current = _smoothedBands[i];
        float target = _spectrumBands[i];

        float speed = target > current ? attackSpeed : releaseSpeed;

        _smoothedBands[i] = Mathf.Lerp(
            current,
            target,
            speed * Time.deltaTime
        );
    }

    void DetectTransients()
    {
        float lastFlux = _fluxBuffer[^1];
        float mu = _fluxBuffer.Average();
        float std = Mathf.Sqrt(_fluxBuffer.Average(d => Mathf.Pow(d - mu, 2)));

        float flux = Flux();

        foreach (KeyValuePair<float, Action<float>> entry in OnTransient.Entries)
        {
            if (entry.Key == 0 || (!useFluxThreshhold && entry.Key <= 1)) continue;
            if (Time.time - _lastTransientTime.GetValueOrDefault(entry.Key) < cooldownSeconds) continue;

            float threshhold = mu + entry.Key * std;

            if (
                (flux > threshhold && flux > lastFlux) ||
                (useFluxThreshhold && flux > entry.Key)
                )
            {
                // Debug.Log($"[Transient Detected] {flux}>{threshhold}(mu={mu};std={std};k={entry.Key})");

                entry.Value?.Invoke(flux);

                _lastTransientTime[entry.Key] = Time.time;
            }
        }

        // Update flux buffer if not skipping
        if (skipSamples == 0 || _sample == 0)
        {
            System.Array.Copy(_fluxBuffer, 1, _fluxBuffer, 0, _fluxBuffer.Length - 1);
            _fluxBuffer[^1] = flux;
        }

        if (skipSamples != 0) _sample = (_sample + 1) % skipSamples;
        else _sample += 1;
    }

    float Flux()
    {
        switch (detectionHeuristic)
        {
            case TransientHeuristic.Flux:
                return bands.Select((bands, i) => Mathf.Max(0f, bands - _prevBands[i])).Sum();
            case TransientHeuristic.Max:
                return bands.Max() - _prevBands.Max();
            case TransientHeuristic.Avg:
                return bands.Average() - _prevBands.Average();
            default:
                return 0f;
        }
    }

    void OnMetronomeTime(float beatFloat, double dspTime)
    {
        foreach (KeyValuePair<float, Action<float, double>> entry in OnBeat.Entries)
        {
            if (entry.Key == 0) continue;

            int beat = Mathf.FloorToInt((beatFloat - 1) / entry.Key);
            if (beat > _lastBeat.GetValueOrDefault(entry.Key, beat - 1))
            {
                _lastBeat[entry.Key] = beat;
                entry.Value?.Invoke(beat, dspTime);
            }
        }
    }
}
