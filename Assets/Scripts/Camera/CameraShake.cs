using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraShake : MonoBehaviour
{
    public float smooth = 1.0f;

    public float m_JitterRange = 5; // fallbacks
    public float m_JitterDuration = 1; //

    private bool m_jitter = false;
    private Vector3 m_startingLocalPosition;
    private Vector3 m_targetPos;

    private void Start()
    {
        m_startingLocalPosition = transform.localPosition;
    }

    private Vector3 RandomVec
    {
        get { return new Vector3(Random.Range(-m_JitterRange, m_JitterRange), Random.Range(-m_JitterRange, m_JitterRange), 0); }
    }

    private void LateUpdate()
    {
        if (m_jitter)
        {
            Jitter(RandomVec);
        }
        else
        {
            m_JitterDuration = 0.5f;
        }
    }

    // Update is called once per frame
    private void Jitter(Vector3 influenctVec)
    {
        if (m_JitterDuration < 0.1f)
        {
            ResetFields();
            return;
        }

        // Create a target position to aim for
        m_targetPos = Vector3.ClampMagnitude(influenctVec, 100);
        // Smoothly go to this target position
        transform.localPosition = Vector3.Lerp(transform.localPosition, m_targetPos, smooth / m_JitterDuration);
        // Decrease jitterFactor over time
        m_JitterDuration -= Time.deltaTime;
        // Make sure jitterFactor reaches zero

    }

    private void ResetFields()
    {
        m_JitterDuration = 0;
        m_jitter = false;
        transform.localPosition = m_startingLocalPosition;
    }

    public void DoJitter(float newJitterDuration, float jitterFactor)
    {
        if (float.IsNaN(newJitterDuration))
        {
            newJitterDuration = 0.15f;
            Debug.LogError("jitterDuration passed isNaN, defaulting to a value of " + newJitterDuration);
        }
        if (m_jitter)
        {
            // ResetFields();
        }
        else
        {
            //Debug.Log("Sart jittering");
            m_jitter = true;
            m_JitterDuration = newJitterDuration;
            m_JitterRange = jitterFactor;
        }
    }
}
