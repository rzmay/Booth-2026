using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class SpectrumData : MonoBehaviour
{
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
    private float[] spectrumBands;
    private float[] smoothedBands;

    [Header("Level Mapping")]
    public bool useDbMapping = true;
    public float dbMin = -80f;
    public float dbMax = -20f;

    public Dictionary<float, Action<float, double>> OnBeat;
    public Dictionary<float, Action<float>> OnTransient; // transient

    private Metronome _metronome;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _metronome = MusicManager.Metronome;

        fftSize = Mathf.NextPowerOfTwo(Mathf.Max(64, numBands, fftSize));
        spectrumBands = new float[numBands];
        smoothedBands = new float[numBands];

        _metronome.OnMetronomeTime += OnMetronomeTime;
    }

    // Update is called once per frame
    void Update()
    {
        // Update fft size
        fftSize = Mathf.NextPowerOfTwo(Mathf.Max(64, numBands, fftSize));

        // Update band count if changed
        if (spectrumBands.Length != numBands || smoothedBands.Length != numBands)
        {
            System.Array.Resize(ref spectrumBands, numBands);
            System.Array.Resize(ref smoothedBands, numBands);
        }

        UpdateSpectrum();
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

        float oldAvg = spectrumBands.Sum() / numBands;
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

            spectrumBands[i] = value;
            SetSmoothedBands(i);
        }

        float max = spectrumBands.Sum();
        float transient = max - oldMax;
        foreach (KeyValuePair<float, Action<float, float>> entry in OnTransient)
        {
            if (transient >= entry.Key) entry.Value?.Invoke(transient);
        }
    }

    // Do this element-wise rather than using a second loop for more speed
    void SetSmoothedBands(int i)
    {
        float current = smoothedBands[i];
        float target = spectrumBands[i];

        float speed = target > current ? attackSpeed : releaseSpeed;

        smoothedBands[i] = Mathf.Lerp(
            current,
            target,
            speed * Time.deltaTime
        );
    }

    void OnMetronomeTime(float beat, double dspTime)
    {
        // TODO: OnBeat
    }
}
