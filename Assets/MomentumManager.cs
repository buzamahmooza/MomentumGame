using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MomentumManager : MonoBehaviour
{
    [SerializeField] private Slider momentumSlider;
    [SerializeField] private Text momentumText;

    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] [Range(0, 10f)] private float decayIntervalInSeconds = 1f;
    [SerializeField] [Range(0, 1f)] private float decayAmount = 0.2f;

    /// <summary> the maximum amount the momentum multiplier can reach </summary>
    [SerializeField] [Range(0, 10f)] private float maxMomentum = 10f;

    /// <summary>
    /// In the beginning of the game, you won't loose momentum.
    /// It's only when you start fighting, then the momentum begins to decay.
    /// This is set to true on the first call of AddMomentum()
    /// </summary>
    private bool _startedGame = false;

    public MomentumManager()
    {
        Momentum = 1;
    }

    public float Momentum { get; private set; }


    void Awake()
    {
        if (momentumSlider != null)
            momentumText = momentumSlider.GetComponentInChildren<Text>();

        if (momentumSlider != null)
            momentumSlider.value = Momentum;
        momentumText.text = "Momentum: x" + Momentum;
    }


    public void AddMomentum(float momentumAdded)
    {
        if (!_startedGame)
        {
            _startedGame = true;
            StartCoroutine(DecayMomentum());
        }

        if (momentumAdded < 0)
        {
            Debug.LogError("AddMomentum() cannot accept negative value: " + momentumAdded);
            return;
        }

        Momentum += momentumAdded;
        Debug.Log("Momentum is now: " + momentumAdded);

        momentumSlider.value = Momentum;
        momentumText.text = "Momentum: x" + Momentum;

        //creating momentum floatingText
        if (floatingTextPrefab)
        {
            GameObject floatingDamageInstance =
                Instantiate(floatingTextPrefab, transform.position, Quaternion.identity);
            FloatingText floatingText = floatingDamageInstance.GetComponent<FloatingText>();
            floatingText.Init(string.Format("+{0}mntm", momentumAdded), momentumText.transform);
            floatingText.text.color = new Color(0, 255, 255, 255);
        }
    }

    IEnumerator DecayMomentum()
    {
        float newVal = Momentum - decayAmount;
        if (newVal <= 0)
        {
            GetComponent<Health>().Die();
            yield return null;
        }

        Momentum = Mathf.Clamp(newVal, 0, maxMomentum);
        yield return new WaitForSeconds(decayIntervalInSeconds);
        StartCoroutine(DecayMomentum());
        yield return null;
    }
}