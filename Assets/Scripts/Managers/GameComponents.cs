using System.Collections;
using System.Collections.Generic;
using Actors.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Helps accessing commonly used script instances and components.
/// </summary>
public class GameComponents : MonoBehaviour
{
    public static readonly int GuiH = 30;

    private static CameraController _cameraController;
    private static TimeManager _timeManager;
    private static ScoreManager _scoreManager;

    public static readonly List<string> Logs = new List<string>();
    public static Music Music;
    [FormerlySerializedAs("pauseMenu")] [SerializeField] private GameObject m_pauseMenu;


    public static GameObject PauseMenu => GameComponents.Instance.m_pauseMenu;

    // todo: add fields that these properties encapsulate, only benefit will be performance
    public static GameObject Player => GameObject.FindWithTag("Player");

    public static GameComponents Instance => FindObjectOfType<GameComponents>();

    public static Rigidbody2D PlayerRb => Player.GetComponent<Rigidbody2D>();

    public static PlayerHealth PlayerHealth => Player == null ? null : Player.GetComponent<PlayerHealth>();

    public static CameraShake CameraShake => Camera.main.GetComponent<CameraShake>();

    public static CameraController CameraController
    {
        get
        {
            _cameraController = _cameraController
                ? _cameraController
                : Camera.main.gameObject.transform.parent.GetComponent<CameraController>();
            return _cameraController;
        }
    }

    public static EnemySpawner EnemySpawner => FindObjectOfType<EnemySpawner>();

    public static TimeManager TimeManager
    {
        get
        {
            _timeManager = _timeManager ? _timeManager : FindObjectOfType<TimeManager>();
            return _timeManager;
        }
    }

    public static AstarPath AstarPath => FindObjectOfType<AstarPath>();

    public static ComboManager ComboManager => FindObjectOfType<ComboManager>();

    public static ScoreManager ScoreManager
    {
        get
        {
            _scoreManager = _scoreManager
                ? _scoreManager
                : FindObjectOfType<ScoreManager>();
            return _scoreManager;
        }
    }

    public static AudioSource AudioSource => GameComponents.Instance.GetComponent<AudioSource>();

    public static RoomBuilder RoomBuilder => FindObjectOfType<RoomBuilder>();


    private void Awake()
    {
        _cameraController = Camera.main.gameObject.transform.parent.GetComponent<CameraController>();
        Music = FindObjectOfType<Music>();
        if (!_timeManager) _timeManager = GetComponent<TimeManager>();

        if (m_pauseMenu == null)
            Debug.LogError("pauseMenu object is not defined");
        m_pauseMenu.SetActive(false);
    }
    
    private void OnGUI()
    {
        GUI.TextArea(new Rect(new Vector2(200, 30), new Vector2(100, 800)), string.Join("\n", Logs.ToArray()),
            new GUIStyle());
    }

    /// <summary>
    /// Adds GUI text and a slider next to the given value,
    /// for this to be responsive, equate the variable to the return of the method
    /// Used for debugging and testing
    /// Note: Must be called within OnGUI()
    /// </summary>
    /// <example>jumpHeight = AddGUISlider("Jump height: ", jumpHeight);</example>
    /// <param name="text"></param>
    /// <param name="value"></param>
    /// <param name="y"></param>
    public static float AddGuiSlider(string text, float value, int y)
    {
        GUI.TextField(
            new Rect(x: 150, y: y, width: 100, height: GameComponents.GuiH),
            text: string.Format("{0}  ({1})", text, value), maxLength: 40,
            style: new GUIStyle()
        );
        return GUI.HorizontalSlider(
            new Rect(x: 25, y: y, width: 100, height: GameComponents.GuiH),
            value,
            0.0f,
            100.0f
        );
    }
}