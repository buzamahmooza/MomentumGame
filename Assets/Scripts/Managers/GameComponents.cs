using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helps accessing commonly used script instances and components.
/// </summary>
public class GameComponents : MonoBehaviour
{
    public static readonly int guiH = 30;

    private static CameraController cameraController;
    [SerializeField] private static TimeManager timeManager;
    [SerializeField] private static ScoreManager scoreManager;

    public static readonly List<string> Logs = new List<string>();
    public static Music Music;
    [SerializeField] private GameObject pauseMenu;


    public static GameObject PauseMenu
    {
        get { return GameComponents.Instance.pauseMenu; }
    }

    // todo: add fields that these properties encapsulate, only benefit will be performance
    public static GameObject Player
    {
        get { return GameObject.FindWithTag("Player"); }
    }

    public static GameComponents Instance
    {
        get { return FindObjectOfType<GameComponents>(); }
    }

    public static Rigidbody2D PlayerRb
    {
        get { return Player.GetComponent<Rigidbody2D>(); }
    }

    public static PlayerHealth PlayerHealth
    {
        get { return Player == null ? null : Player.GetComponent<PlayerHealth>(); }
    }

    public static CameraShake CameraShake
    {
        get { return Camera.main.GetComponent<CameraShake>(); }
    }

    public static CameraController CameraController
    {
        get
        {
            cameraController = cameraController
                ? cameraController
                : Camera.main.gameObject.transform.parent.GetComponent<CameraController>();
            return cameraController;
        }
    }

    public static EnemySpawner EnemySpawner
    {
        get { return FindObjectOfType<EnemySpawner>(); }
    }

    public static TimeManager TimeManager
    {
        get
        {
            timeManager = timeManager ? timeManager : FindObjectOfType<TimeManager>();
            return timeManager;
        }
    }

    public static AstarPath AstarPath
    {
        get { return FindObjectOfType<AstarPath>(); }
    }
    
    public static ComboManager ComboManager
    {
        get { return FindObjectOfType<ComboManager>(); }
    }

    public static ScoreManager ScoreManager
    {
        get
        {
            scoreManager = scoreManager
                ? scoreManager
                : FindObjectOfType<ScoreManager>();
            return scoreManager;
        }
    }

    public static AudioSource AudioSource
    {
        get { return GameComponents.Instance.GetComponent<AudioSource>(); }
    }

    public static RoomBuilder RoomBuilder
    {
        get { return FindObjectOfType<RoomBuilder>(); }
    }


    private void Awake()
    {
        cameraController = Camera.main.gameObject.transform.parent.GetComponent<CameraController>();
        Music = FindObjectOfType<Music>();
        if (!timeManager) timeManager = GetComponent<TimeManager>();

        if (pauseMenu == null)
            Debug.LogError("pauseMenu object is not defined");
        pauseMenu.SetActive(false);
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
    public static float AddGUISlider(string text, float value, int y)
    {
        GUI.TextField(
            new Rect(x: 150, y: y, width: 100, height: GameComponents.guiH),
            text: string.Format("{0}  ({1})", text, value), maxLength: 40,
            style: new GUIStyle()
        );
        return GUI.HorizontalSlider(
            new Rect(x: 25, y: y, width: 100, height: GameComponents.guiH),
            value,
            0.0f,
            100.0f
        );
    }
}