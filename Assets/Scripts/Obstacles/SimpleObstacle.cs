using UnityEngine;

public class SimpleObstacle : Obstacle
{
    public float speed = 5f;
    public float destroyAfter = 6f;
    public Vector3 moveDirection = Vector3.forward;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        Destroy(gameObject, destroyAfter);
    }
    // Update is called once per frame
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 dir = Vector3.Scale(moveDirection.normalized, transform.forward.normalized);
        rb.linearVelocity = dir * speed;
    }

    protected override void OnHit(Collision collision)
    {
        Destroy(gameObject);
    }
}
