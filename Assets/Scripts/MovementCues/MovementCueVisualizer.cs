using UnityEngine;

[RequireComponent(typeof(MovementCue))]
public class MovementCueVisualizer : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private SpriteRenderer _glowRenderer;
    [SerializeField] private Renderer _ringRenderer;
    [SerializeField] private SpriteRenderer _handSpriteRenderer;

    [SerializeField] private Sprite _leftHandSprite;
    [SerializeField] private Sprite _rightHandSprite;

    public float perfectRingRadius;
    public float maxRingRadius;
    public float minRingRadius;

    public Gradient meshGradient = new Gradient();
    public Gradient glowGradient = new Gradient();
    public Gradient ringGradient = new Gradient();

    private MovementCue _movementCue;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _movementCue = GetComponent<MovementCue>();

        _handSpriteRenderer.sprite = _movementCue.hand == Hand.Side.Left ? _leftHandSprite : _rightHandSprite;
    }

    // Update is called once per frame
    void Update()
    {
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

        _meshRenderer.materials[0].color = color;
    }

    void UpdateGlow(float progress)
    {
        Color color = glowGradient.Evaluate(progress / 2f);

        _glowRenderer.materials[0].color = color;
    }
}
