using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonDebug : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    public void OnPointerEnter(PointerEventData eventData) => Debug.Log($"{name} PointerEnter");
    public void OnPointerExit(PointerEventData eventData) => Debug.Log($"{name} PointerExit");
    public void OnPointerDown(PointerEventData eventData) => Debug.Log($"{name} PointerDown");
    public void OnPointerUp(PointerEventData eventData) => Debug.Log($"{name} PointerUp");
    public void OnSelect(BaseEventData eventData) => Debug.Log($"{name} Select");
    public void OnDeselect(BaseEventData eventData) => Debug.Log($"{name} Deselect");
    public void OnSubmit(BaseEventData eventData) => Debug.Log($"{name} Submit");
}
