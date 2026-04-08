using UnityEngine;


public class SimpleObstacle : Obstacle
{
    public float destroyAfter = 6f;
    private Rigidbody rb;
    [SerializeField] public float speed = 1f;
    [SerializeField] public float spawnDistance = 10f;
    public override float scheduleAhead => spawnDistance / speed;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        Vector3 dir = transform.forward;
        float lateBy = Mathf.Max(0f, Time.time - startTime);
        transform.position += dir * speed * lateBy;
        Destroy(gameObject, destroyAfter);
    }
    // Update is called once per frame
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    private void FixedUpdate()
    {
        rb.linearVelocity = transform.forward * speed;
    }


    protected override void OnHit(Collision collision)
    {
        Destroy(gameObject);
    }
}
