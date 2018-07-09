using System;
using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField]
    float slowDownLength = 2.0f;
    [SerializeField]
    [Range(0.001f, 1)]
    float slowdownFactor = 0.05f,
        defaultHitStopDuration = 0.05f;

    [SerializeField] private ParticleSystem ps;
    private bool m_SloMo = false;
    public float hSliderValue = 1000;

    void Awake() {
        if (!ps) ps = GetComponent<ParticleSystem>();
    }

    public void DoSlowMotion() {
        DoSlowMotion(slowdownFactor);
    }

    public void DoSlowMotion(float theSlowdownFactor) {
        Time.timeScale = Mathf.Clamp(theSlowdownFactor, 0f, 100f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        m_SloMo = true;
    }
    /// <summary>
    /// slows down time for a duration of 0.05f seconds
    /// </summary>
    public void DoHitStop() {
        DoHitStop(defaultHitStopDuration);
    }
    public void DoHitStop(float seconds) {
        Time.timeScale = 0.01f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        StartCoroutine(ResetTimeScale(seconds));
    }

    private IEnumerator ResetTimeScale(float seconds) {
        yield return StartCoroutine(CoroutineUtil.WaitForRealSeconds(seconds));
        ResetTimeScale();
    }

    // Update is called once per frame
    private void Update() {
        if (m_SloMo) {
            ps.Emit(1);
            if (Math.Abs((Time.timeScale) - 1) < 0.01f) {
                ResetTimeScale();
            }

            Time.timeScale = Mathf.Clamp(Time.timeScale + 1 / slowDownLength * Time.unscaledDeltaTime, 0f, 1f);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        //Mathf.Lerp(Time.timeScale, 1, Time.deltaTime);
    }

    private void ResetTimeScale() {
        m_SloMo = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = Time.fixedUnscaledDeltaTime;
    }
}
public static class CoroutineUtil
{
    public static IEnumerator WaitForRealSeconds(float time) {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup < start + time) {
            yield return null;
        }
    }
}