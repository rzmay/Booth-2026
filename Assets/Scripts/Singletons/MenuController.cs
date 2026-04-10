using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MenuController : DelayableMonoBehaviour
{
    private static MenuController _Instance;

    [SerializeField] private float _menuLerpFactor = 5f;

    [SerializeField] private List<GameObject> _menuUI; // Expecting 2 -- level select, game over
    [SerializeField] private List<GameObject> _menuObjects; // Any menu objects that need to be deactivated on change
    public int initialMenu = -1; // Default to no menu active
    [SerializeField] private float _acceptRestartDelay;
    [SerializeField] private InputActionReference _restartAction;
    [SerializeField] private InputActionReference _overrideRestartAction;
    public string restartSceneName;

    private List<bool> _menuActive;

    private bool _acceptRestart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _Instance = this;
    }

    void Start()
    {
        if (_restartAction != null) _restartAction.action.performed += OnRestartAction;
        if (_overrideRestartAction != null) _overrideRestartAction.action.performed += OnOverrideRestartAction;

        _menuActive = new(new bool[_menuUI.Count]);

        // Set initial menu -- no menu
        _SetMenu(initialMenu);
    }

    void OnDestroy()
    {
        if (_restartAction != null) _restartAction.action.performed -= OnRestartAction;
        if (_overrideRestartAction != null) _overrideRestartAction.action.performed -= OnOverrideRestartAction;
    }

    // Update is called once per frame
    void Update()
    {
        // Lerp menu opacity
        for (int i = 0; i < _menuUI.Count; i++)
        {
            CanvasGroup group = _menuUI[i].GetComponent<CanvasGroup>();
            if (!group) continue;

            group.interactable = _menuActive[i];
            group.blocksRaycasts = _menuActive[i];

            float targetAlpha = _menuActive[i] ? 1f : 0f;
            if (!Mathf.Approximately(group.alpha, targetAlpha))
            {
                group.alpha = Mathf.Lerp(group.alpha, targetAlpha, _menuLerpFactor * Time.deltaTime);
            }
            else
            {
                group.alpha = targetAlpha;
            }


            if (_menuObjects[i] != null) _menuObjects[i].SetActive(_menuActive[i]);
        }
    }

    void _SetMenu(int index)
    {
        for (int i = 0; i < _Instance._menuUI.Count; i++)
        {
            _menuActive[i] = i == index;
        }

        // If it's game over, accept restart input
        if (index == 2)
        {
            Delay(() =>
            {
                _acceptRestart = true;
            }, _acceptRestartDelay);
        }
    }

    void OnRestartAction(InputAction.CallbackContext obj)
    {
        if (_acceptRestart)
        {
            // Destroy the calibration manager
            Destroy(CalibrationManager.Instance);

            SceneManager.LoadScene(restartSceneName);
        }
    }

    void OnOverrideRestartAction(InputAction.CallbackContext obj)
    {
        // Destroy the calibration manager
        Destroy(CalibrationManager.Instance);

        SceneManager.LoadScene(restartSceneName);
    }

    public void LoadLevel(int index)
    {
        // Recalibrate location before loading the next level
        CalibrationManager.ResetLocation();

        SceneManager.LoadScene($"Level{index}");
    }

    public static void SetMenu(int index)
    {
        _Instance._SetMenu(index);
    }
}
