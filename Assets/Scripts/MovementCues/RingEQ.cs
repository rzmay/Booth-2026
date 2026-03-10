using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CircleLineRenderer))]
public class RingEQ : MonoBehaviour
{
    [Header("Audio Inputs")]
    public List<AudioSource> sources = new List<AudioSource>();
    public bool useListener = false;

    [Header("Spectrum Settings")]
    public int fftSize = 1024;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    public bool useLogScaling = true;

    public float startFrequencyRange = 0.0f;
    public float endFrequencyRange = 1.0f;

    [Header("Visualization")]
    public float amplitude = 0.75f;
    public float power = 1.25f;
    public bool mirror = false;

    [Header("Smoothing")]
    [Tooltip("Whether or not to use smoothing")]
    public bool useSmoothing = true;

    [Tooltip("How fast the visual responds when increasing")]
    public float attackSpeed = 50f;

    [Tooltip("How fast the visual falls when decreasing")]
    public float releaseSpeed = 10f;
    private float[] spectrumBands;
    private float[] smoothedBands;

    [Header("Wave")]
    [Tooltip("Whether or not to use a wave")]
    public bool useWave = true;
    [Tooltip("Frequency of the wave")]
    public int waveFrequency = 12;
    [Tooltip("How many beats to switch wave phase on (0 = off)")]
    public int phaseFrequency = 1;
    [Tooltip("Scale wave frequency with amplitude?")]
    public bool scaleFrequencyWithAmplitude = false;
    [Tooltip("Scale wave frequency with spectrum frequency")]
    public bool scaleFrequencyWithSpectrum = true;
    [Tooltip("Change phase on transients?")]
    public bool useTransients = true;
    [Tooltip("Transient detection threshhold")]
    public float transientThreshhold = 0.5f; // TODO: tune this value

    [Header("Level Mapping")]
    public bool useDbMapping = true;
    public float dbMin = -80f;
    public float dbMax = -20f;

    private CircleLineRenderer circle;

    private int _phase = 0;     // 1 or 0
    private float _phaseSmooth = 0;

    void Awake()
    {
        circle = GetComponent<CircleLineRenderer>();

        fftSize = Mathf.NextPowerOfTwo(Mathf.Max(64, fftSize));
    }

    void Start()
    {
        MusicManager.Metronome.OnBeat += OnBeat;

        // Ensure fftSize >= resolution
        fftSize = Mathf.NextPowerOfTwo(
            Mathf.Max(circle.resolution, fftSize)
        );

        spectrumBands = new float[circle.resolution];
        smoothedBands = new float[circle.resolution];

        circle.SetOffsetFunction(GetOffset);
    }

    void Update()
    {
        _phaseSmooth = Mathf.Lerp(_phaseSmooth, _phase, attackSpeed * Time.deltaTime);

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
        int bandCount = spectrumBands.Length;
        int n = spectrumRaw.Length;

        // Precompute centers (bin indices)
        int[] centers = new int[bandCount];
        int stepLinear = Mathf.Max(1, n / bandCount);

        for (int i = 0; i < bandCount; i++)
        {
            if (!useLogScaling)
            {
                centers[i] = Mathf.Clamp(i * stepLinear + stepLinear / 2, 0, n - 1);
            }
            else
            {
                float t = (bandCount == 1) ? 0f : (float)i / (bandCount - 1);
                float logIndex = Mathf.Pow(t, 2f) * (n - 1);
                centers[i] = Mathf.Clamp(Mathf.RoundToInt(logIndex), 0, n - 1);
            }
        }

        // Ensure centers are non-decreasing (can happen with rounding in log mode)
        for (int i = 1; i < bandCount; i++)
            centers[i] = Mathf.Max(centers[i], centers[i - 1]);

        float oldMax = spectrumBands.Max();
        for (int i = 0; i < bandCount; i++)
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
                int rightMid = (i == bandCount - 1) ? n : (centers[i] + centers[i + 1]) / 2;

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

        float max = spectrumBands.Max();
        if (max - oldMax >= transientThreshhold && useTransients) SwitchPhase();
    }

    // Do this element-wise rather than using a second loop for more speed
    private void SetSmoothedBands(int i)
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

    public float GetOffset(float t)
    {
        float[] bands = useSmoothing ? smoothedBands : spectrumBands;

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
            Mathf.FloorToInt(m * (bands.Length - 1)),
            0,
            bands.Length - 1
        );

        float value = bands[index];

        value = Mathf.Pow(value, power);

        float waveVal = 1f;
        if (useWave)
        {
            float phaseMultiplier = 1f;

            if (scaleFrequencyWithAmplitude) phaseMultiplier *= value;
            if (scaleFrequencyWithSpectrum) phaseMultiplier *= 1 + (index * 2) / (bands.Length - 1);

            waveVal = Mathf.Sin(t * 2 * Mathf.PI * waveFrequency * phaseMultiplier);
            waveVal *= (_phaseSmooth * 2) - 1;
        }

        return value * amplitude * waveVal;
    }

    void OnBeat(int beat, double _)
    {
        if (phaseFrequency == 0) return;
        if (beat % phaseFrequency == 0) SwitchPhase();
    }

    void SwitchPhase()
    {
        _phase = 1 - _phase;
    }
}
