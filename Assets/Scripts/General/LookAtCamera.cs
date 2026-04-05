using UnityEngine;
using UnityEngine.Animations;


[RequireComponent(typeof(LookAtConstraint))]
public class LookAtCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LookAtConstraint lookAt = GetComponent<LookAtConstraint>();

        // Look at the player (center eye camera)
        lookAt.AddSource(new ConstraintSource() { sourceTransform = Player.Instance.transform, weight = 1.0f });
    }
}
