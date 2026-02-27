using UnityEngine;

public sealed class FollowMouse : MonoBehaviour
{
    [SerializeField] private float z = 0f;
    [SerializeField] private Camera cam;

    void Start()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        if (!cam) return;

        Vector3 mouse = Input.mousePosition;

        // Set screen-space depth so ScreenToWorldPoint lands on the desired Z plane
        float depth = Mathf.Abs(cam.transform.position.z - z);
        mouse.z = depth;

        Vector3 world = cam.ScreenToWorldPoint(mouse);
        world.z = z;

        transform.position = world;
    }
}
