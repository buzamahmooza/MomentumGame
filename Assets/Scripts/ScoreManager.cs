using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text scoreText;
    private float currentScore = 0;
    [SerializeField] [Range(0, 100)] private float scoreAdded2MomentumPercent;

    // Use this for initialization
    void Start() {
        UpdateScore();
    }

    // Update is called once per frame
    void Update() {

    }
    /// <summary>
    /// Adds the score to the current score value and updates the score text
    /// Also adds momentum with a value of x% of the addedScore
    /// </summary>
    /// <param name="addedScore"></param>
    public void AddScore(float addedScore) {
        if (addedScore < 0) addedScore = 0;
        GameManager.Player.GetComponent<PlayerMove>().AddMomentum(addedScore * scoreAdded2MomentumPercent / 100.0f);

        currentScore += addedScore;
        UpdateScore();
    }

    private void UpdateScore() {
        System.Diagnostics.Debug.Assert(scoreText != null, "scoreText != null");
        if (scoreText) scoreText.text = "Score: " + currentScore;
    }
}
