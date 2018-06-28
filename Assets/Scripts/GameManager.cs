using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static CameraController cameraController;
    [SerializeField] private static TimeManager timeManager;

    // todo: add fields that these properties encapsulate, only benefit will be performance
    public static GameObject Player { get { return GameObject.FindWithTag("Player"); } }
    public static Rigidbody2D PlayerRb { get { return Player.GetComponent<Rigidbody2D>(); } }
    public static PlayerHealth PlayerHealth { get { return Player == null ? null : Player.GetComponent<PlayerHealth>(); } }
    public static bool PlayerIsDead { get { return PlayerHealth.isDead; } }
    public static CameraShake CameraShake { get { return Camera.main.GetComponent<CameraShake>(); } }
    public static CameraController CameraController {
        get {
            return cameraController ?? Camera.main.gameObject.transform.parent.GetComponent<CameraController>();
        }
    }
    public static TimeManager TimeManager { get { return timeManager = timeManager ?? GameObject.Find("Game Controller").GetComponent<TimeManager>(); } }

    private static void Awake() {
        cameraController = Camera.main.gameObject.transform.parent.GetComponent<CameraController>();
    }
    private void Start() {
        if (!timeManager) timeManager = GetComponent<TimeManager>();
    }

    private void Update() {
        //Press 'R' to restart level
        if (Input.GetKeyDown(KeyCode.R)) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        Debug.Assert(Player != null);
        Debug.Assert(PlayerHealth != null);
    }
}
