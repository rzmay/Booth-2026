using UnityEngine;
using UnityEngine.Animations;


public class LookAtCamera : MonoBehaviour
{
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - Player.Instance.transform.position);
    }
}
