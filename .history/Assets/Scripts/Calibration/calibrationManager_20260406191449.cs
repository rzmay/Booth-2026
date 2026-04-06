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
    private bool _doCalib = false;
    private bool _resetLocn = false;

    public static CalibrationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void LateUpdate()
    {
        if (_doCalib)
        {
            Calibrate();
            _doCalib = false;
        }
        if (_resetLocn)
        {
            ResetLocn();
            _resetLocn = false;
        }
    }

    private void ResetLocn()
    {
        // re-get the location and rotation of the headset, but keep the same scale
        Vector3 headsetPosition = cameraRig.centerEyeAnchor.position;
        Vector3 rotationEuler = (cameraRig.centerEyeAnchor.rotation * Quaternion.Euler(rotationOffsetEuler)).eulerAngles;
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
    }
    private void Calibrate()
    {
        currentCalibration.isCalibrated = false;
        currentCalibration.originPosition = Vector3.zero;
        currentCalibration.originRotation = Quaternion.identity;
        currentCalibration.scale = 1f;
        Vector3 headsetPosition = cameraRig.centerEyeAnchor.position;

        float leftArmLength = Vector3.Distance(headsetPosition, Player.Instance.leftHand.transform.position);
        float rightArmLength = Vector3.Distance(headsetPosition, Player.Instance.rightHand.transform.position);
        float armLength = Mathf.Max(leftArmLength, rightArmLength);

        Vector3 rotationEuler = (cameraRig.centerEyeAnchor.rotation * Quaternion.Euler(rotationOffsetEuler)).eulerAngles;

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
            Debug.Log("Calibration not done yet, returning uncalibrated position");
            return position;
        }

        Debug.Log("Converting normalized position " + position + " to world position using calibration: " + currentCalibration);
        Vector3 newPos = position * currentCalibration.scale;

        return currentCalibration.originPosition + currentCalibration.originRotation * newPos;
    }

    public void SetCalibrationBool(bool value)
    {
        currentCalibration.isCalibrated = value;
    }

    public void doCalibrationExternal()
    {
        _doCalib = true;
    }
    public void doResetLocnExternal()
    {
        _resetLocn = true;
    }
}
