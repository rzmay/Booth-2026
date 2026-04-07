using Oculus.Interaction;
using UnityEngine;

[RequireComponent(typeof(PointableCanvas))]
public class PointableCanvasEventLogger : MonoBehaviour
{
  private PointableCanvas _pointableCanvas;

  void Awake()
  {
    _pointableCanvas = GetComponent<PointableCanvas>();
  }

  void OnEnable()
  {
    _pointableCanvas.WhenPointerEventRaised += OnPointerEventRaised;
  }

  void OnDisable()
  {
    if (_pointableCanvas != null)
      _pointableCanvas.WhenPointerEventRaised -= OnPointerEventRaised;
  }

  private void OnPointerEventRaised(PointerEvent evt)
  {
    if (evt.Type != PointerEventType.Move) Debug.Log(
        $"[PC EVT] id={evt.Identifier} type={evt.Type} pos={evt.Pose.position}");
  }
}
