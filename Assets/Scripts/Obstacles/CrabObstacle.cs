using UnityEngine;

public class CrabObstacle : Obstacle
{
    public float speed = 5f;
    public Vector3 moveDirection = Vector3.forward;
    public float destroyAfter = 6f;

    private Animator animator; 

    private Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        animator.Play("Claw_Attack");
        Destroy(gameObject, destroyAfter);
    }
    // Update is called once per frame
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDirection.normalized * speed;
    }

    protected override void OnHit(Collision collision)
    {
        Destroy(gameObject);
    }
}
