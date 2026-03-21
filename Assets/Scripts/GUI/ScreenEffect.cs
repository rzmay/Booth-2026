using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ScreenEffect : MonoBehaviour
{
  public float smoothing = 1f;
  public AnimationCurve dilationPower;

  private float _beat;
  private float _smoothAmount = 0f;
  private RawImage _image;

  void Awake()
  {
    _image = GetComponent<RawImage>();
  }

  void Start()
  {
    MusicManager.Metronome.OnMetronomeTime += OnMetronomeTime;

    // Start with no dilation
    _image.material.SetFloat("Beat", 0.99f);
  }

  void Update()
  {
    float progress = StreakTracker.Instance.streakProgress / 4.0f;

    // Amount tracks to progress
    _smoothAmount = Mathf.Lerp(_smoothAmount, progress, smoothing * Time.deltaTime);

    // Set material amount
    _image.material.SetFloat("Amount", _smoothAmount);

    // Set dilation power by progress
    _image.material.SetFloat("DilationPower", dilationPower.Evaluate(_smoothAmount));

    // Set dilation if begun
    if (_beat >= 0)
    {
      Debug.Log($"Setting Beat={_beat % 1f}");
    }
  }

  public void OnMetronomeTime(float beatFloat, double _)
  {
    _beat = beatFloat;
  }
}
