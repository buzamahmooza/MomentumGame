using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text scoreText;
    private float currentScore = 0;

    // Use this for initialization
    void Start() {
        UpdateScore();
    }

    // Update is called once per frame
    void Update() {

    }

    public void AddScore(float addedScore) {
        if (addedScore < 0) addedScore = 0;

        currentScore += addedScore;
        UpdateScore();
    }

    private void UpdateScore() {
        System.Diagnostics.Debug.Assert(scoreText != null, "scoreText != null");
        if (scoreText) scoreText.text = "Score: " + currentScore;
    }
}
