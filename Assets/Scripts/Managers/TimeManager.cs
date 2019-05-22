using System;
using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] float slowDownLength = 2.0f;

    [SerializeField] [Range(0.001f, 1)] float
        slowdownFactor = 0.05f,
        defaultHitStopDuration = 0.05f;

    [SerializeField] private GameObject _slomoParticles;

    private bool m_SloMo = false;
    private float _lastTimescale = 1;


    private void Update()
    {
        if (m_SloMo)
        {
            if (Math.Abs(Time.timeScale - 1) < 0.01f)
            {
                ResetTimeScale();
            }

            Time.timeScale = Mathf.Clamp(Time.timeScale + 1 / slowDownLength * Time.unscaledDeltaTime, 0f, 1f);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    public void TogglePause()
    {
        // if timescale == 0 (paused)
        if (Math.Abs(Time.timeScale) < 0.01F) 
        {
            // unpause
            Time.timeScale = _lastTimescale;
            GameComponents.PauseMenu.SetActive(false);
        }
        else
        {
            // pause
            _lastTimescale = Time.timeScale;
            Time.timeScale = 0;
            GameComponents.PauseMenu.SetActive(true);
        }
    }

    public void DoSlowMotion()
    {
        DoSlowMotion(slowdownFactor);
    }

    public void DoSlowMotion(float theSlowdownFactor)
    {
        Time.timeScale = Mathf.Clamp(theSlowdownFactor, 0f, 100f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        m_SloMo = true;

        // spawn the particles with the main camera
        if (_slomoParticles)
            Instantiate(_slomoParticles, Camera.main.transform.position + Vector3.forward, Quaternion.identity,
                FindObjectOfType<CameraController>().transform);
    }

    /// <summary>
    /// slows down time for a duration of 0.05f seconds
    /// </summary>
    public void DoHitStop()
    {
        DoHitStop(defaultHitStopDuration);
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
        m_SloMo = false;
        Time.timeScale = _lastTimescale;
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