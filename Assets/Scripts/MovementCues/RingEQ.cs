using System.Collections.Generic;
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

    [Header("Visualization")]
    public float amplitude = 1f;
    public float power = 1.5f;

    [Header("Smoothing")]
    [Tooltip("Whether or not to use smoothing")]
    public bool useSmoothing = true;

    [Tooltip("How fast the visual responds when increasing")]
    public float attackSpeed = 50f;

    [Tooltip("How fast the visual falls when decreasing")]
    public float releaseSpeed = 10f;

    private float[] spectrumRaw;
    private float[] spectrumBands;
    private float[] smoothedBands;

    private CircleLineRenderer circle;

    private void Awake()
    {
        circle = GetComponent<CircleLineRenderer>();

        fftSize = Mathf.NextPowerOfTwo(Mathf.Max(64, fftSize));
    }

    private void Start()
    {
        // Ensure fftSize >= resolution
        fftSize = Mathf.NextPowerOfTwo(
            Mathf.Max(circle.resolution, fftSize)
        );

        spectrumRaw = new float[fftSize];
        spectrumBands = new float[circle.resolution];
        smoothedBands = new float[circle.resolution];

        circle.SetOffsetFunction(GetOffset);
    }

    private void Update()
    {
        UpdateSpectrum();
    }

    public void UpdateSpectrum()
    {
        if (useListener)
        {
            AudioListener.GetSpectrumData(spectrumRaw, 0, fftWindow);
        }
        else
        {
            System.Array.Clear(spectrumRaw, 0, spectrumRaw.Length);

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

        BuildBands();
    }

    private void BuildBands()
    {
        int bandCount = spectrumBands.Length;

        if (!useLogScaling)
        {
            int step = spectrumRaw.Length / bandCount;

            for (int i = 0; i < bandCount; i++)
            {
                float sum = 0f;
                int start = i * step;
                int end = start + step;

                for (int j = start; j < end; j++)
                    sum += spectrumRaw[j];

                spectrumBands[i] = sum / step;

                SetSmoothedBands(i);
            }
        }
        else
        {
            // Logarithmic scaling
            for (int i = 0; i < bandCount; i++)
            {
                float t = (float)i / bandCount;
                float logIndex = Mathf.Pow(t, 2f) * (spectrumRaw.Length - 1);
                int index = Mathf.Clamp(Mathf.FloorToInt(logIndex), 0, spectrumRaw.Length - 1);

                spectrumBands[i] = spectrumRaw[index];

                SetSmoothedBands(i);
            }
        }
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

        // Shift so top (0.25) becomes zero
        float shifted = (t - 0.25f + 1f) % 1f;

        // Mirror 0→1→0
        float mirrored = shifted <= 0.5f
            ? shifted * 2f
            : (1f - shifted) * 2f;

        int index = Mathf.Clamp(
            Mathf.FloorToInt(mirrored * (bands.Length - 1)),
            0,
            bands.Length - 1
        );

        float value = bands[index];

        // Exaggerate peaks
        value = Mathf.Pow(value, power);

        return value * amplitude;
    }
}
