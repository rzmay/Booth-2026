using UnityEngine;

[System.Serializable]
public class CalibrationData
{
    public bool isCalibrated;
    public Vector3 originPosition = Vector3.zero;
    public Quaternion originRotation = Quaternion.identity;
    public Vector3 worldScale = Vector3.one;
}
