using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class NextIndicationLine : MonoBehaviour
{
  [Header("Line Points")]
  [SerializeField] public int numPoints = 16;
  [SerializeField] public Vector3 startPoint;
  [SerializeField] public Vector3 endPoint = Vector3.forward;

  [Header("Animation")]
  [SerializeField] private float speed = 1f; // normalized 0->1 per second
  [SerializeField] private float width = 0.1f;

  [Header("Trail Shape")]
  [SerializeField] private float trailLength = 0.2f;
  [SerializeField] private float fadeAfterHead = 0.05f;
  [SerializeField, Range(0f, 1f)] private float trailAlpha = 0.35f;

  private float _offset;
  private LineRenderer _lineRenderer;

  private void Awake()
  {
    _lineRenderer = GetComponent<LineRenderer>();
  }

  private void Start()
  {
    _lineRenderer.useWorldSpace = true;
    _offset = 0f;

    ApplyLinePositions();
    ApplyProceduralGradient();
  }

  private void Update()
  {
    _offset += speed * Time.deltaTime;

    // Only destroy once even the tail has fully moved past the end.
    if (_offset - trailLength >= 1f)
    {
      Destroy(gameObject);
      return;
    }

    ApplyLinePositions();
    ApplyProceduralGradient();
  }

  private void ApplyLinePositions()
  {
    if (_lineRenderer == null)
      return;

    _lineRenderer.startWidth = width;
    _lineRenderer.endWidth = width;

    int count = Mathf.Max(2, numPoints);
    _lineRenderer.positionCount = count;

    for (int i = 0; i < count; i++)
    {
      float t = (float)i / (count - 1);
      Vector3 pos = Vector3.Lerp(startPoint, endPoint, t);
      _lineRenderer.SetPosition(i, pos);
    }
  }

  private void ApplyProceduralGradient()
  {
    if (_lineRenderer == null)
      return;

    float head = _offset;
    float trailStart = Mathf.Max(0f, head - trailLength);
    float trailEnd = head + fadeAfterHead;

    Gradient gradient = new Gradient();

    GradientColorKey[] colorKeys = new GradientColorKey[]
    {
        new GradientColorKey(Color.white, 0f),
        new GradientColorKey(Color.white, 1f),
    };

    GradientAlphaKey[] alphaKeys;

    if (head <= 1f)
    {
      alphaKeys = new GradientAlphaKey[]
      {
            new GradientAlphaKey(0f, trailStart),
            new GradientAlphaKey(trailAlpha, head),
            new GradientAlphaKey(0f, Mathf.Min(1f, trailEnd)),
      };
    }
    else
    {
      // Head is off the line; only the fading tail remains.
      float remainingPeakAlpha = Mathf.InverseLerp(trailEnd, head, 1f);

      alphaKeys = new GradientAlphaKey[]
      {
            new GradientAlphaKey(0f, trailStart),
            new GradientAlphaKey(remainingPeakAlpha, 1f),
      };
    }

    gradient.SetKeys(colorKeys, alphaKeys);
    _lineRenderer.colorGradient = gradient;
  }
}
