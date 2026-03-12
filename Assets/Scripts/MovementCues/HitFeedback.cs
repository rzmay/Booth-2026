using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HitFeedback : MonoBehaviour
{
    public float lifetime = 1.5f;
    public AnimationCurve sizeOverTime;
    public MovementCue.ResultMap<List<string>> feedbackText;

    [SerializeField] private Image _imageMask;
    public Sprite maskImage;

    [SerializeField] private TMP_Text _text;

    private RectTransform _rectTransform;
    private float _showTime = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rectTransform = _imageMask.GetComponent<RectTransform>();

        // Hide
        _rectTransform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (_showTime < 0) return;

        _rectTransform.localScale = Vector3.one * sizeOverTime.Evaluate((Time.time - _showTime) / lifetime);

        if (Time.time - _showTime > lifetime)
        {
            Destroy(gameObject);
        }
    }

    public void Show(MovementCue.Result result)
    {
        List<string> textOptions = feedbackText[result];

        _imageMask.sprite = result == MovementCue.Result.Miss ? maskImage : null;
        _text.text = textOptions.Count > 0 ? textOptions[Random.Range(0, textOptions.Count)].ToUpper() : "";

        _showTime = Time.time;
    }
}
