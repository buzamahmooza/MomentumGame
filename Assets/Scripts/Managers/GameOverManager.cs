using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private AudioClip missionFailedClip;
    
    private void Start()
    {
        GameComponents.TimeManager.ResetTimeScale();
    }

    private void OnEnable()
    {
        GameComponents.PlayerHealth.OnDeath += () =>
        {
            GameComponents.Music.GetComponent<AudioSource>().Stop();
            if (missionFailedClip)
                GameComponents.AudioSource.PlayOneShot(missionFailedClip);
            
            Invoke("Restart", 5);
        };
    }

    void Restart()
    {
        GameManager.RestartScene();
    }
}