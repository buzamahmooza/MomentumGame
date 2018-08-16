using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MomentumManager : MonoBehaviour
{
    [SerializeField] private Slider momentumSlider;
    [SerializeField] private Text momentumText;

    [SerializeField] private GameObject floatingTextPrefab;

    [SerializeField] [Range(0, 1f)] private float decayAmount = 0.05f;

    /// <summary> the maximum amount the momentum multiplier can reach </summary>
    [SerializeField] [Range(0, 5f)] public float MaxMomentum = 2.5f;

    [SerializeField] [Range(0, 100)] private float minimumMomentumPercentForTrail = 70f;

    /// <summary>
    /// In the beginning of the game, you won't loose momentum.
    /// It's only when you start fighting, then the momentum begins to decay.
    /// This is set to true on the first call of AddMomentum()
    /// </summary>
    private bool m_startedGame = false;
    private TrailGenerator m_trailGenerator;


    public MomentumManager()
    {
        Momentum = 1;
    }

    void Awake()
    {
        if (momentumSlider != null)
            momentumText = momentumSlider.GetComponentInChildren<Text>();

        if (momentumSlider != null)
        {
            momentumSlider.value = Momentum;
            momentumSlider.maxValue = MaxMomentum;
        }

        m_trailGenerator = GetComponent<TrailGenerator>();

        UpdateText();
    }

    void Update()
    {
        // enable when over 50%
        m_trailGenerator.SampleLifetime = Momentum > MaxMomentum * minimumMomentumPercentForTrail / 100 ? 1 : 0;
    }

    public float Momentum { get; private set; }

    public void AddMomentum(float momentumAdded)
    {
        // if not yet started counting, the counting starts now.
        if (!m_startedGame)
        {
            StartCoroutine(WaitAndDecayMomentum());
            m_startedGame = true;
        }


        if (momentumAdded < 0)
        {
            Debug.LogError("AddMomentum() cannot accept negative value: " + momentumAdded);
            return;
        }

        Momentum += momentumAdded;

        UpdateMomentum();

        //creating momentum floatingText
        if (floatingTextPrefab)
        {
            GameObject floatingDamageInstance =
                Instantiate(floatingTextPrefab, transform.position, Quaternion.identity);
            FloatingText floatingText = floatingDamageInstance.GetComponent<FloatingText>();
            floatingText.Init(
                string.Format("+{0} mntm", momentumAdded),
                momentumText.transform.position
            );
            floatingText.text.color = new Color(0, 255, 255, 255);
        }
    }

    /// <summary> Updates the momentum GUI (text and slider) </summary>
    private void UpdateMomentum()
    {
        momentumSlider.value = Momentum;
        UpdateText();
    }

    private void UpdateText()
    {
        momentumText.text = "Momentum: x" + Momentum;
    }

    void DecreaseMomentum(float amount)
    {
        float newVal = Momentum - amount;
        if (newVal <= 0)
        {
            GetComponent<Health>().Die();
            return;
        }

        Momentum = Mathf.Clamp(newVal, 0, MaxMomentum);
        UpdateMomentum();
    }

    IEnumerator WaitAndDecayMomentum()
    {
        // Set this to the score average of live enemies in the room
        float[] livingEnemyScores = GameObject.FindObjectsOfType<EnemyHealth>().Select(
            enemyHealth => enemyHealth.enabled && !enemyHealth.IsDead
                ? (float) enemyHealth.ScoreValue
                : 0
        ).Where(c => c > 0).ToArray();

        // The interval in which the player should be killing enemies without loosing momentum (Seconds/Kill).
        float expectedKillInterval = livingEnemyScores.Sum() /
                                     (livingEnemyScores.Length < 1
                                         ? 1
                                         : livingEnemyScores.Length) * // the average score
                                     GameManager.ScoreManager.scoreAdded2MomentumPercent;
        // never go below 1
        if (expectedKillInterval < 1)
            expectedKillInterval = 1;

        print(string.Format("You are expected to kill one enemy every {0} seconds", expectedKillInterval));
        DecreaseMomentum(decayAmount / expectedKillInterval);

        yield return new WaitForSeconds(1f);
        StartCoroutine(WaitAndDecayMomentum());
        yield return null;
    }
}