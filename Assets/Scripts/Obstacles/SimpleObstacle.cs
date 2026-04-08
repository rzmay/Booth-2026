using UnityEngine;

public class SimpleObstacle : Obstacle
{
    public float destroyAfter = 6f;
    public Vector3 moveDirection = Vector3.forward;
    private Rigidbody rb;
    [SerializeField] public float speed = 1f;
    [SerializeField] public float maxSpawnDistance = 10f;
    public override float scheduleAhead => maxSpawnDistance / speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        Vector3 dir = transform.TransformDirection(moveDirection.normalized);
        Destroy(gameObject, destroyAfter);
    }
    // Update is called once per frame
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 dir = transform.TransformDirection(moveDirection.normalized);
        rb.linearVelocity = dir * speed;
    }

    protected override void OnHit(Collision collision)
    {
        Destroy(gameObject);
    }
}
