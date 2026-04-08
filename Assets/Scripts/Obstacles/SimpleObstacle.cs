using Oculus.Haptics;
using UnityEngine;

public class SimpleObstacle : Obstacle
{
    public float destroyAfter = 6f;
    [SerializeField] public float speed = 1f;
    [SerializeField] public float spawnDistance = 10f;
    public float hitReward = 1f;
    public float minHitStrength = 0.0001f;
    public float hitSpeedMultiplier = 3f;
    public bool lookAtPlayer = false;
    [SerializeField] private HapticClip hapticClip;

    public override float scheduleAhead => spawnDistance / speed;

    private Rigidbody _rb;
    private bool _hit = false;
    private Vector3 _moveDirection;
    private float _currentSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Vector3 toPlayer = Player.Instance.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(toPlayer);

        _moveDirection = toPlayer.normalized;
        _currentSpeed = speed;

        Destroy(gameObject, destroyAfter);
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _moveDirection * _currentSpeed;
    }

    private void LateUpdate()
    {
        if (_hit || !lookAtPlayer) return;

        Vector3 toPlayer = Player.Instance.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(toPlayer);
    }

    protected override void OnHit(Collision collision)
    {
        if (_hit) return;

        Vector3 impulse = collision.impulse;
        if (impulse.sqrMagnitude < minHitStrength) return;

        StreakTracker.Instance.streak += hitReward;

        _hit = true;

        Vector3 hitDir;

        if (impulse.sqrMagnitude > 0.0001f)
        {
            // Use actual collision response direction if available
            hitDir = impulse.normalized;
        }
        else
        {
            // Fallback: away from the thing that hit us
            hitDir = (transform.position - collision.transform.position).normalized;
        }

        _moveDirection = hitDir;
        _currentSpeed = speed * hitSpeedMultiplier;

        HapticSource hapticSource = collision.gameObject.GetComponent<HapticSource>();
        if (hapticSource != null && hapticClip != null)
        {
            hapticSource.clip = hapticClip;
            hapticSource.Play();
        }
    }
}
