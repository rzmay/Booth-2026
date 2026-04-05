using TMPro;
using UnityEngine;

public class ScheduledText : Schedulable
{
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowPowerId = Shader.PropertyToID("_GlowPower");

    [Header("Text")]
    [SerializeField] private TMP_Text _text;
    [SerializeField, TextArea] private string _message = "";
    [SerializeField] private Color _color = Color.white;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float _lifespan = 1f;
    [SerializeField, Min(0f)] private float _fadeTime = 0.5f;

    [Header("Glow")]
    [SerializeField] private bool _useGlow = false;
    [SerializeField] private Color _glowColor = Color.white;
    [SerializeField, Min(0f)] private float _glowPower = 0.2f;

    public override float scheduleAhead => 0f;

    private Color _baseColor;
    private Color _baseGlowColor;
    private Material _materialInstance;

    void Reset()
    {
        _text = GetComponentInChildren<TMP_Text>();
    }

    void Awake()
    {
        if (_text == null)
        {
            _text = GetComponentInChildren<TMP_Text>();
        }
    }

    void Start()
    {
        if (_text == null)
        {
            Debug.LogWarning($"ScheduledText on {name} could not find a TMP_Text reference.");
            Destroy(gameObject);
            return;
        }

        _baseColor = _color;
        _baseGlowColor = _glowColor;

        _text.text = _message;
        ApplyVisuals(1f);
        ConfigureGlow();
    }

    void Update()
    {
        if (_text == null) return;

        float elapsed = Time.time - startTime;
        if (elapsed < 0f) return;

        if (elapsed < _lifespan)
        {
            ApplyVisuals(1f);
            return;
        }

        if (_fadeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float fadeProgress = Mathf.InverseLerp(_lifespan, _lifespan + _fadeTime, elapsed);
        ApplyVisuals(1f - fadeProgress);

        if (elapsed >= _lifespan + _fadeTime)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }

    private void ApplyVisuals(float alphaScale)
    {
        Color textColor = _baseColor;
        textColor.a *= Mathf.Clamp01(alphaScale);
        _text.color = textColor;

        if (_materialInstance != null && _materialInstance.HasProperty(GlowColorId))
        {
            Color glowColor = _baseGlowColor;
            glowColor.a *= Mathf.Clamp01(alphaScale);
            _materialInstance.SetColor(GlowColorId, glowColor);
        }
    }

    private void ConfigureGlow()
    {
        if (!_useGlow) return;

        Material sourceMaterial = _text.fontSharedMaterial;
        if (sourceMaterial == null) return;
        if (!sourceMaterial.HasProperty(GlowColorId) || !sourceMaterial.HasProperty(GlowPowerId)) return;

        _materialInstance = new Material(sourceMaterial);
        _materialInstance.SetColor(GlowColorId, _baseGlowColor);
        _materialInstance.SetFloat(GlowPowerId, _glowPower);
        _text.fontMaterial = _materialInstance;
    }
}
