using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameOverManager : MonoBehaviour
{
    [FormerlySerializedAs("missionFailedClip")] [SerializeField] private AudioClip m_missionFailedClip;
    
    private void Start()
    {
        GameComponents.TimeManager.ResetTimeScale();
    }

    private void OnEnable()
    {
        GameComponents.PlayerHealth.OnDeath += () =>
        {
            GameComponents.Music.GetComponent<AudioSource>().Stop();
            if (m_missionFailedClip)
                GameComponents.AudioSource.PlayOneShot(m_missionFailedClip);
            
            Invoke(nameof(Restart), 5);
        };
    }

    void Restart()
    {
        GameManager.RestartScene();
    }
}