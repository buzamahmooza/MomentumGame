using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetButtonDown("Menu"))
        {
            GameComponents.TimeManager.TogglePause();
        }


        //restart level
        if (Input.GetButtonDown("Restart"))
        {
            RestartScene();
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            GameComponents.Music.PlayNext();
        }
    }

    public static void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1f;
//            Time.fixedDeltaTime = Time.fixedUnscaledDeltaTime;
        GameComponents.TimeManager.ResetTimeScale();
    }
}