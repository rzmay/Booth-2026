using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovementCue))]
public class MovementCueVisualizer : MonoBehaviour
{
    [Header("Main Rendering")]
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private SpriteRenderer _glowRenderer;
    [SerializeField] private Renderer _ringRenderer;
    [SerializeField] private SpriteRenderer _handSpriteRenderer;

    public float perfectRingRadius;
    public float maxRingRadius;
    public float minRingRadius;

    [SerializeField] private Sprite _leftHandSprite;
    [SerializeField] private Sprite _rightHandSprite;

    [Header("Next Up Indication")]
    [SerializeField] private Renderer _nextUpRing;
    public float nextUpGlowScale = 2f;
    public float nextUpGlowOpacity = 1.5f;
    public float nextUpSmoothing = 10f;
    [SerializeField] NextIndicationLine nextLinePrefab;

    [Header("Result Visualization")]
    [SerializeField] private HitFeedback _hitFeedback;
    [SerializeField] private MovementCue.ResultMap<ParticleSystem> _particleSystems;


    public Gradient meshGradient = new Gradient();
    public Gradient glowGradient = new Gradient();
    public Gradient ringGradient = new Gradient();

    private MovementCue _movementCue;
    private float _minParticleAlpha = 0f;

    private Vector3 _initGlowScale;
    private float _glowAlphaScale = 1f;


    void Awake()
    {
        _movementCue = GetComponent<MovementCue>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _initGlowScale = _glowRenderer.transform.localScale;

        _handSpriteRenderer.sprite = _movementCue.hand == Hand.Side.Left ? _leftHandSprite : _rightHandSprite;

        // Particle alpha should be at minimum the same as glow alpha when the cue is no longer on time
        _minParticleAlpha = glowGradient.Evaluate(
            (_movementCue.earlyWindow + _movementCue.onTimeWindow) / _movementCue.hitWindow
            ).a;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNextUp();

        float progress = _movementCue.hitWindowProgress;

        UpdateRing(progress);
        UpdateMesh(progress);
        UpdateGlow(progress);
    }

    void UpdateRing(float progress)
    {
        float radius = progress <= 1f
            ? Mathf.Lerp(maxRingRadius, perfectRingRadius, progress)
            : Mathf.Lerp(perfectRingRadius, minRingRadius, progress - 1f);

        Color color = ringGradient.Evaluate(progress / 2f);

        _ringRenderer.materials[0].color = color;
        _ringRenderer.transform.localScale = Vector3.one * radius;
    }

    void UpdateMesh(float progress)
    {
        Color color = meshGradient.Evaluate(progress / 2f);
        color.a *= _glowAlphaScale;

        _meshRenderer.materials[0].color = color;
    }

    void UpdateGlow(float progress)
    {
        Color color = glowGradient.Evaluate(progress / 2f);

        _glowRenderer.color = color;
    }

    void UpdateNextUp()
    {
        // Set glow scale
        _glowRenderer.transform.localScale = Vector3.Lerp(
            _glowRenderer.transform.localScale,
            _initGlowScale * (_movementCue.isNext ? nextUpGlowScale : 1),
            nextUpSmoothing * Time.deltaTime
        );

        // Set glow alpha boost
        _glowAlphaScale = Mathf.Lerp(
            _glowAlphaScale,
            _movementCue.isNext ? nextUpGlowOpacity : 1.0f,
            nextUpSmoothing * Time.deltaTime
        );

        // Set ring scale
        _nextUpRing.transform.localScale = Vector3.Lerp(
            _nextUpRing.transform.localScale,
            _movementCue.isNext ? Vector3.one : Vector3.zero,
            nextUpSmoothing * Time.deltaTime
        );
    }

    public void VisualizeResult(MovementCue.Result result)
    {
        ParticleSystem particleSystem = _particleSystems[result];
        ParticleSystem[] particles = particleSystem.GetComponentsInChildren<ParticleSystem>();

        // Match color on all particles to be played
        foreach (var ps in particles)
        {
            Color particleColor = glowGradient.Evaluate(_movementCue.hitWindowProgress / 2f);
            particleColor.a = Mathf.Max(_minParticleAlpha, particleColor.a);

            var main = ps.main;
            main.startColor = particleColor;
        }

        // Play particle system
        particleSystem.Play();

        // Show feedback
        _hitFeedback.Show(result);
    }

    public void ShowNextLine(List<MovementCue> previous)
    {
        foreach (var p in previous)
        {
            if (p == null) continue;

            NextIndicationLine line = Instantiate(nextLinePrefab);
            line.startPoint = p.transform.position;
            line.endPoint = transform.position;
        }
    }
}
