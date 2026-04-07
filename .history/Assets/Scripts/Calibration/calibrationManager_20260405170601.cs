using UnityEngine;

public class CalibrationManager : MonoBehaviour
{
    [SerializeField] private CalibrationSettings settings;
    [SerializeField] private CalibrationData currentCalibration = new();

    public CalibrationData CurrentCalibration => currentCalibration;

    public void ComputeCalibration()
    {
        Transform headset = Player.Instance.transform;
        Vector3 headsetPosition = headset.position;

        float leftArmLength = Vector3.Distance(headsetPosition, Player.Instance.leftHand.transform.position);
        float rightArmLength = Vector3.Distance(headsetPosition, Player.Instance.rightHand.transform.position);
        float armLength = Mathf.Max(leftArmLength, rightArmLength);

        Vector3 rotationEuler = (headset.rotation * Quaternion.Euler(settings.rotationOffsetEuler)).eulerAngles;

        if (settings.freezeRotationX)
        {
            rotationEuler.x = 0f;
        }

        if (settings.freezeRotationY)
        {
            rotationEuler.y = 0f;
        }

        if (settings.freezeRotationZ)
        {
            rotationEuler.z = 0f;
        }

        currentCalibration.originPosition = headsetPosition;
        currentCalibration.originRotation = Quaternion.Euler(rotationEuler);
        currentCalibration.worldScale = Vector3.one * armLength;
        currentCalibration.isCalibrated = true;
    }

    public Vector3 ConvertNormalizedToWorldPosition(Vector3 normalizedPosition)
    {
        Vector3 localPosition = Vector3.Scale(normalizedPosition, currentCalibration.worldScale) + settings.positionOffset;
        return currentCalibration.originPosition + currentCalibration.originRotation * localPosition;
    }
}
