using Oculus.Interaction;
using UnityEngine;

public class PointableCanvasHoverLogger : MonoBehaviour
{
  void OnEnable()
  {
    PointableCanvasModule.WhenSelectableHovered += OnHovered;
    PointableCanvasModule.WhenSelectableUnhovered += OnUnhovered;
  }

  void OnDisable()
  {
    PointableCanvasModule.WhenSelectableHovered -= OnHovered;
    PointableCanvasModule.WhenSelectableUnhovered -= OnUnhovered;
  }

  private void OnHovered(PointableCanvasEventArgs e)
  {
    Debug.Log(
        $"[PCM HOVER] canvas={e.Canvas?.name} hovered={e.Hovered?.name ?? "<null>"} dragging={e.Dragging} pointerId={e.PointerId}");
  }

  private void OnUnhovered(PointableCanvasEventArgs e)
  {
    Debug.Log(
        $"[PCM UNHOVER] canvas={e.Canvas?.name} hovered={e.Hovered?.name ?? "<null>"} dragging={e.Dragging} pointerId={e.PointerId}");
  }
}
