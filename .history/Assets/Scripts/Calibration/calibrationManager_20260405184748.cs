using UnityEngine;

public class CalibrationManager : BoolValueSource
{
    [Header("Normalized Space")]
    [SerializeField] private Vector3 normalizedMin = Vector3.zero;
    [SerializeField] private Vector3 normalizedMax = Vector3.one;

    [Header("Offsets")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;

    [Header("Rotation Constraints")]
    [SerializeField] private bool freezeRotationX = true;
    [SerializeField] private bool freezeRotationY = false;
    [SerializeField] private bool freezeRotationZ = true;

    [SerializeField] private CalibrationData currentCalibration = new();

    public CalibrationData CurrentCalibration => currentCalibration;
    public override bool Value => currentCalibration.isCalibrated;

    public void Calibrate()
    {
        Transform headset = Player.Instance.transform;
        Vector3 headsetPosition = headset.position;

        float leftArmLength = Vector3.Distance(headsetPosition, Player.Instance.leftHand.transform.position);
        float rightArmLength = Vector3.Distance(headsetPosition, Player.Instance.rightHand.transform.position);
        float armLength = Mathf.Max(leftArmLength, rightArmLength);

        Vector3 rotationEuler = (headset.rotation * Quaternion.Euler(rotationOffsetEuler)).eulerAngles;

        if (freezeRotationX)
        {
            rotationEuler.x = 0f;
        }

        if (freezeRotationY)
        {
            rotationEuler.y = 0f;
        }

        if (freezeRotationZ)
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
        if (!currentCalibration.isCalibrated)
        {
            Debug.LogWarning("calibration not yet set");
            return normalizedPosition;
        }
        Vector3 localPosition = new(
            normalizedPosition.x * currentCalibration.worldScale.x,
            normalizedPosition.y,
            normalizedPosition.z * currentCalibration.worldScale.z
        );

        localPosition += positionOffset;
        return currentCalibration.originPosition + currentCalibration.originRotation * localPosition;
    }
}
