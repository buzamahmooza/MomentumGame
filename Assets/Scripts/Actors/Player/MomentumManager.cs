using System.Collections;
using System.Linq;
using Actors.Enemy;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Actors.Player
{
    public class MomentumManager : MonoBehaviour
    {
        [SerializeField] private Slider m_momentumSlider;
        private Text m_momentumText;

        [SerializeField] private GameObject m_floatingTextPrefab;

        [SerializeField] [Range(0, 1f)] private float m_decayAmount = 0.05f;

        /// <summary> the maximum amount the momentum multiplier can reach </summary>
        [SerializeField] [Range(0, 5f)] public float MaxMomentum = 2.5f;

        [SerializeField] [Range(0, 100)] private float m_minimumMomentumPercentForTrail = 70f;

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
            if (m_momentumSlider != null)
                m_momentumText = m_momentumSlider.GetComponentInChildren<Text>();

            if (m_momentumSlider != null)
            {
                m_momentumSlider.value = Momentum;
                m_momentumSlider.maxValue = MaxMomentum;
            }

            m_trailGenerator = GetComponent<TrailGenerator>();

            UpdateText();
        }

        void Update()
        {
            // enable when over 50%
            m_trailGenerator.SampleLifetime = Momentum > MaxMomentum * m_minimumMomentumPercentForTrail / 100 ? 1 : 0;
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
            if (m_floatingTextPrefab)
            {
                GameObject floatingDamageInstance =
                    Instantiate(m_floatingTextPrefab, transform.position, Quaternion.identity);
                FloatingText floatingText = floatingDamageInstance.GetComponent<FloatingText>();
                floatingText.Init(
                    string.Format("+{0} mntm", momentumAdded),
                    m_momentumText.transform.position
                );
                floatingText.text.color = new Color(0, 255, 255, 255);
            }
        }

        /// <summary> Updates the momentum GUI (text and slider) </summary>
        private void UpdateMomentum()
        {
            m_momentumSlider.value = Momentum;
            UpdateText();
        }

        private void UpdateText()
        {
            m_momentumText.text = "Momentum: x" + Momentum;
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
            var livingEnemyScores = FindObjectsOfType<EnemyHealth>().Where(enemyHealth =>
                enemyHealth.enabled && !enemyHealth.IsDead && enemyHealth.ScoreValue > 0
            ).Select(eh => eh.ScoreValue).ToArray();

            // The interval in which the player should be killing enemies without loosing momentum (Seconds/Kill).
            float expectedKillInterval =
                // the average score
                Mathf.Max(
                    1,
                    livingEnemyScores.Sum() / Mathf.Max(1, livingEnemyScores.Length)
                )
                * GameComponents.ScoreManager.ScoreAdded2MomentumPercent;

            float expectedKillFrequency = 1 / expectedKillInterval;

            print(string.Format("You are expected to kill one enemy every {0} seconds", 1 / expectedKillFrequency));
            DecreaseMomentum(m_decayAmount * expectedKillFrequency);

            yield return new WaitForSeconds(1f);
            StartCoroutine(WaitAndDecayMomentum());

            yield return null;
        }
    }
}