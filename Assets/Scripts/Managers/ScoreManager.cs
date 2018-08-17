using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text scoreText;
    private float currentScore = 0;
    [SerializeField] [Range(0, 100)] public float scoreAdded2MomentumPercent = 0.15f;

    private PlayerAttack _playerAttack;
    private MomentumManager _momentumManager;

    void Awake()
    {
        _playerAttack = GameComponents.Player.GetComponent<PlayerAttack>();
        _momentumManager = GameComponents.Player.GetComponent<MomentumManager>();
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
        _momentumManager.AddMomentum(
            addedScore * (scoreAdded2MomentumPercent * 0.01f) /
            Mathf.Clamp(Mathf.Log(_momentumManager.Momentum), 1, _momentumManager.MaxMomentum)
        );
        // divide by Log(momentum) so that enemies give less momentum the more you have

        if (multiplyByCombo && _playerAttack.CurrentComboInstance != null)
        {
            addedScore *= _playerAttack.CurrentComboInstance.Count;
        }

        currentScore += addedScore;
        UpdateScore();
    }

    private void UpdateScore()
    {
        System.Diagnostics.Debug.Assert(scoreText != null, "scoreText != null");
        if (scoreText) scoreText.text = "Score: " + currentScore;
    }
}