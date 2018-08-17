using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private AudioClip missionFailedClip;
    
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
            GameComponents.Music.PlayNext();
        }
    }    
 
    private void OnEnable()
    {
        GameComponents.PlayerHealth.OnDeath += () =>
        {
            GameComponents.Music.GetComponent<AudioSource>().Stop();
            if (missionFailedClip)
                GameComponents.AudioSource.PlayOneShot(missionFailedClip);
        };
    }
}