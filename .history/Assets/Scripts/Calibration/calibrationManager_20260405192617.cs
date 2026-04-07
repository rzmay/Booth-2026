using UnityEngine;

public class CalibrationManager : BoolValueSource
{

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
        float armLength = Mathf.Min(leftArmLength, rightArmLength);

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
        currentCalibration.scale = armLength;
        currentCalibration.isCalibrated = true;
    }

    public Vector3 ConvertNormalizedToWorldPosition(Vector3 position)
    {
        if (!currentCalibration.isCalibrated)
        {
            Debug.LogWarning("calibration not yet set");
            return position;
        }
        Debug.Log("ARM LENGTH BEING RECORDED: " + currentCalibration.scale);

        Vector3 newPos = position * currentCalibration.scale;

        return currentCalibration.originPosition + currentCalibration.originRotation * newPos;
    }
}
