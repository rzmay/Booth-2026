using UnityEngine;

public class Spinner : MonoBehaviour
{
    public Vector3 rotationStep;
    public float rotationSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotationStep * rotationSpeed * Time.deltaTime);
    }
}
