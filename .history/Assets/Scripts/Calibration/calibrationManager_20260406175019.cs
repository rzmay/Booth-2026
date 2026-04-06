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
    [SerializeField] private OVRCameraRig cameraRig;

    public CalibrationData CurrentCalibration => currentCalibration;
    public override bool Value => currentCalibration.isCalibrated;

    public void Calibrate()
    {
        Debug.Log("CALIBRATE BEING CALLED");
        currentCalibration.isCalibrated = false;
        currentCalibration.originPosition = Vector3.zero;
        currentCalibration.originRotation = Quaternion.identity;
        currentCalibration.scale = 1f;
        Debug.Log("CALIBRATION RESET");
        Debug.Log("PICKING PLAYER TRANSFORM AS ORIGIN");
        Vector3 headsetPosition = cameraRig.centerEyeAnchor.position;
        Debug.Log("HEADSET POSITION IS: " + headsetPosition);

        float leftArmLength = Vector3.Distance(headsetPosition, Player.Instance.leftHand.transform.position);
        float rightArmLength = Vector3.Distance(headsetPosition, Player.Instance.rightHand.transform.position);
        float armLength = Mathf.Max(leftArmLength, rightArmLength);
        // debug logs of left arm, right arm
        Debug.Log("LEFT ARM LENGTH: " + leftArmLength);
        Debug.Log("RIGHT ARM LENGTH: " + rightArmLength);

        Vector3 rotationEuler = (cameraRig.centerEyeAnchor.rotationEuler * Quaternion.Euler(rotationOffsetEuler)).eulerAngles;

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

        Debug.Log("SETTING ORIGIN POSITION TO: " + headsetPosition);
        Debug.Log("SETTING ORIGIN ROTATION TO: " + rotationEuler);
        Debug.Log("SETTING SCALE TO: " + armLength);

        currentCalibration.originPosition = headsetPosition;
        currentCalibration.originRotation = Quaternion.Euler(rotationEuler);
        currentCalibration.scale = armLength;
        Debug.Log("ARM LENGTH BEING RECORDED: " + currentCalibration.scale);
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

        Debug.Log("ORIGIN POSITION IS: " + currentCalibration.originPosition);
        Debug.Log("ORIGIN ROTATION IS: " + currentCalibration.originRotation);
        Debug.Log("SCALE IS: " + currentCalibration.scale);
        Debug.Log("returning position: " + (currentCalibration.originPosition + currentCalibration.originRotation * newPos));
        return currentCalibration.originPosition + currentCalibration.originRotation * newPos;
    }

    public void SetCalibrationBool(bool value)
    {
        Debug.Log("SET CALIBRATION BOOL BEING CALLED");
        currentCalibration.isCalibrated = value;
    }
}
