using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class TimeManager : MonoBehaviour
{
    [FormerlySerializedAs("slowDownLength")] [SerializeField] float m_slowDownLength = 2.0f;

    [FormerlySerializedAs("slowdownFactor")] [SerializeField] [Range(0.001f, 1)] float
        m_slowdownFactor = 0.05f;

    [FormerlySerializedAs("defaultHitStopDuration")] [SerializeField] [Range(0.001f, 1)] float
        m_defaultHitStopDuration = 0.05f;

    [FormerlySerializedAs("_slomoParticles")] [SerializeField] private GameObject m_slomoParticles;

    private bool m_sloMo = false;
    private float m_lastTimescale = 1;


    private void Update()
    {
        if (m_sloMo)
        {
            if (Math.Abs(Time.timeScale - 1) < 0.01f)
            {
                ResetTimeScale();
            }

            Time.timeScale = Mathf.Clamp(Time.timeScale + 1 / m_slowDownLength * Time.unscaledDeltaTime, 0f, 1f);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    public void TogglePause()
    {
        // if timescale == 0 (paused)
        if (Math.Abs(Time.timeScale) < 0.01F) 
        {
            // unpause
            Time.timeScale = m_lastTimescale;
            GameComponents.PauseMenu.SetActive(false);
        }
        else
        {
            // pause
            m_lastTimescale = Time.timeScale;
            Time.timeScale = 0;
            GameComponents.PauseMenu.SetActive(true);
        }
    }

    public void DoSlowMotion()
    {
        DoSlowMotion(m_slowdownFactor);
    }

    public void DoSlowMotion(float theSlowdownFactor)
    {
        Time.timeScale = Mathf.Clamp(theSlowdownFactor, 0f, 100f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        m_sloMo = true;

        // spawn the particles with the main camera
        if (m_slomoParticles)
            Instantiate(m_slomoParticles, Camera.main.transform.position + Vector3.forward, Quaternion.identity,
                FindObjectOfType<CameraController>().transform);
    }

    /// <summary>
    /// slows down time for a duration of 0.05f seconds
    /// </summary>
    public void DoHitStop()
    {
        DoHitStop(m_defaultHitStopDuration);
    }

    public void DoHitStop(float seconds)
    {
        Time.timeScale = 0.01f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        StartCoroutine(ResetTimeScale(seconds));
    }

    private IEnumerator ResetTimeScale(float seconds)
    {
        yield return StartCoroutine(CoroutineUtil.WaitForRealSeconds(seconds));
        ResetTimeScale();
    }

    public void ResetTimeScale()
    {
        m_sloMo = false;
        Time.timeScale = m_lastTimescale;
        Time.fixedDeltaTime = 0.02f;
    }
}

public static class CoroutineUtil
{
    public static IEnumerator WaitForRealSeconds(float time)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup < start + time)
        {
            yield return null;
        }
    }
}