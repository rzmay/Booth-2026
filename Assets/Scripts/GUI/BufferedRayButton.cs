using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BufferedRayButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
  [Header("Input")]
  [SerializeField] private InputActionReference selectAction;

  [Header("Timing")]
  [SerializeField] private float hoverGraceSeconds = 0.15f;
  [SerializeField] private bool refreshGraceWhileStillHovered = true;

  private Button _button;
  private Graphic _targetGraphic;

  private Color _normalColor;
  private Color _highlightedColor;
  private bool _hasColorTintTransition;

  private bool _isPointerInside;
  private bool _graceActive;
  private float _graceEndTime;

  void Awake()
  {
    _button = GetComponent<Button>();
    _targetGraphic = _button.targetGraphic;

    var colors = _button.colors;
    _normalColor = colors.normalColor;
    _highlightedColor = colors.highlightedColor;
    _hasColorTintTransition = _button.transition == Selectable.Transition.ColorTint;
  }

  void OnEnable()
  {
    if (selectAction != null && selectAction.action != null)
    {
      selectAction.action.performed += OnSelectPerformed;
      selectAction.action.Enable();
    }

    ClearVisualState();
  }

  void OnDisable()
  {
    if (selectAction != null && selectAction.action != null)
    {
      selectAction.action.performed -= OnSelectPerformed;
    }

    ClearVisualState();
  }

  void Update()
  {
    if (_graceActive && Time.unscaledTime >= _graceEndTime)
    {
      EndGraceWindow();
    }
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    _isPointerInside = true;
    StartGraceWindow();
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    _isPointerInside = false;

    // Intentionally do nothing here.
    // We keep the grace window alive until its timer expires.
  }

  private void OnSelectPerformed(InputAction.CallbackContext context)
  {
    if (!_button.interactable)
      return;

    if (!_graceActive)
      return;

    _button.onClick.Invoke();
    EndGraceWindow();
  }

  private void StartGraceWindow()
  {
    if (_graceActive && !refreshGraceWhileStillHovered)
      return;

    _graceActive = true;
    _graceEndTime = Time.unscaledTime + hoverGraceSeconds;
    ApplyHoverVisual();
  }

  private void EndGraceWindow()
  {
    _graceActive = false;
    ClearVisualState();
  }

  private void ApplyHoverVisual()
  {
    if (!_hasColorTintTransition || _targetGraphic == null)
      return;

    // Also change button colors in case it tries to override ours
    var colors = _button.colors;
    colors.normalColor = _highlightedColor;
    _button.colors = colors;
  }

  private void ClearVisualState()
  {
    if (!_hasColorTintTransition || _targetGraphic == null)
      return;

    var colors = _button.colors;
    colors.normalColor = _normalColor;
    _button.colors = colors;
  }
}
