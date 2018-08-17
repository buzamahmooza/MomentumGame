using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    
    private void Start()
    {
        GameComponents.TimeManager.ResetTimeScale();
    }

    private void Update()
    {
        //restart level
        if (Input.GetButtonDown("Restart"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = Time.fixedUnscaledDeltaTime;
            GameComponents.TimeManager.ResetTimeScale();
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            GameComponents.Instance.music.PlayNext();
        }
    }

    private void OnGUI()
    {
        GUI.TextArea(new Rect(new Vector2(200, 30), new Vector2(100, 800)), string.Join("\n", logs.ToArray()),
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