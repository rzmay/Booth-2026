using UnityEngine;

public class SphereObstacle : Obstacle
{
    public float speed = 5f;
    public Vector3 moveDirection = Vector3.forward;
    public float destroyAfter = 6f;

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
        rb.linearVelocity = moveDirection.normalized * speed;
        Vector3 axis = Vector3.Cross(moveDirection, Vector3.up).normalized;
        rb.angularVelocity = axis * speed;
    }

    protected override void OnHit(Collision collision)
    {
        Destroy(gameObject);
    }
}
