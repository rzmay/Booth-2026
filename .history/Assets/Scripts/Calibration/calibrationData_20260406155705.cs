using UnityEngine;

// serializeable class meaning Unity can edit values
[System.Serializable]
public class calibrationData
{
    public bool isCalibrated;
    public Vector3 originPosition = Vector3.zero;
    public Quaternion originRotation = Quaternion.identity;
    public float scale = 1f;
}
