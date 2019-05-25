using System.Collections;
using System.Collections.Generic;
using Actors;
using Actors.Player;
using UnityEngine;
using UnityEngine.Serialization;

public class ScoreManager : MonoBehaviour
{
    [FormerlySerializedAs("scoreText")] [SerializeField] private UnityEngine.UI.Text m_scoreText;
    private float m_currentScore = 0;
    [FormerlySerializedAs("scoreAdded2MomentumPercent")] [SerializeField] [Range(0, 100)] public float ScoreAdded2MomentumPercent = 0.15f;

    private PlayerAttack m_playerAttack;
    private MomentumManager m_momentumManager;

    void Awake()
    {
        m_playerAttack = GameComponents.Player.GetComponent<PlayerAttack>();
        m_momentumManager = GameComponents.Player.GetComponent<MomentumManager>();
    }

    // Use this for initialization
    void Start()
    {
        UpdateScore();
    }

    /// <summary>
    /// Adds the score to the current score value and updates the score text
    /// Also adds momentum with a value of x% of the addedScore
    /// </summary>
    /// <param name="addedScore"></param>
    /// <param name="multiplyByCombo"></param>
    public void AddScore(float addedScore, bool multiplyByCombo = false)
    {
        if (addedScore < 0) addedScore = 0;
        m_momentumManager.AddMomentum(
            addedScore * (ScoreAdded2MomentumPercent * 0.01f) /
            Mathf.Clamp(Mathf.Log(m_momentumManager.Momentum), 1, m_momentumManager.MaxMomentum)
        );
        // divide by Log(momentum) so that enemies give less momentum the more you have

        if (multiplyByCombo && m_playerAttack.CurrentComboInstance != null)
        {
            addedScore *= m_playerAttack.CurrentComboInstance.Count;
        }

        m_currentScore += addedScore;
        UpdateScore();
    }

    private void UpdateScore()
    {
        System.Diagnostics.Debug.Assert(m_scoreText != null, "scoreText != null");
        if (m_scoreText) m_scoreText.text = "Score: " + m_currentScore;
    }
}