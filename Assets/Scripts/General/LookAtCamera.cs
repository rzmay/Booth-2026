using UnityEngine;
using UnityEngine.Animations;


[RequireComponent(typeof(LookAtConstraint))]
public class LookAtCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LookAtConstraint lookAt = GetComponent<LookAtConstraint>();

        lookAt.AddSource(new ConstraintSource() { sourceTransform = Camera.main.transform, weight = 1.0f });
    }
}
