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

    [SerializeField] private List<GameObject> _menus; // Expecting 2 -- level select, game over
    [SerializeField] private float _acceptRestartDelay;
    [SerializeField] private InputActionReference _restartAction;
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
        _restartAction.action.performed += OnRestartAction;
        _menuActive = new(new bool[_menus.Count]);

        // Set initial menu -- no menu
        _SetMenu(-1);
    }

    void OnDestroy()
    {
        _restartAction.action.performed -= OnRestartAction;
    }

    // Update is called once per frame
    void Update()
    {
        // Lerp menu opacity
        for (int i = 0; i < _menus.Count; i++)
        {
            CanvasGroup group = _menus[i].GetComponent<CanvasGroup>();
            if (!group) continue;

            float targetAlpha = _menuActive[i] ? 1f : 0f;
            if (!Mathf.Approximately(group.alpha, targetAlpha))
            {
                group.alpha = Mathf.Lerp(group.alpha, targetAlpha, _menuLerpFactor * Time.deltaTime);
            }
            else
            {
                group.alpha = targetAlpha;
            }
        }
    }

    void _SetMenu(int index)
    {
        for (int i = 0; i < _Instance._menus.Count; i++)
        {
            _menuActive[i] = i == index;
        }

        // If it's game over, accept restart input
        if (index == 1)
        {
            Delay(() =>
            {
                _acceptRestart = true;
            }, _acceptRestartDelay);
        }
    }

    void OnRestartAction(InputAction.CallbackContext obj)
    {
        if (_acceptRestart) SceneManager.LoadScene(restartSceneName);
    }

    public static void SetMenu(int index)
    {
        _Instance._SetMenu(index);
    }
}
