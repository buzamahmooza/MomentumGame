using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 m_TargetPos;
    public float smooth = 1.0f;

    public float m_JitterRange = 5; // fallbacks
    public float m_JitterDuration = 1; //

    private bool m_Jitter = false;
    private Vector3 startingLocalPosition;

    private void Start() {
        startingLocalPosition = transform.localPosition;
    }

    public Vector3 RandomVec {
        get { return new Vector3(Random.Range(-m_JitterRange, m_JitterRange), Random.Range(-m_JitterRange, m_JitterRange), 0); }
    }

    private void LateUpdate() {
        if (m_Jitter) {
            Jitter(RandomVec);
        } else {
            m_JitterDuration = 0.5f;
        }
    }

    // Update is called once per frame
    private void Jitter(Vector3 influenctVec) {
        // Create a target position to aim for
        m_TargetPos = influenctVec;
        // Smoothly go to this target position
        transform.localPosition = Vector3.Lerp(transform.localPosition, m_TargetPos, smooth / m_JitterDuration);

        // Decrease jitterFactor over time
        m_JitterDuration -= Time.deltaTime;
        // Make sure jitterFactor reaches zero
        if (m_JitterDuration < 0.1f) {
            ResetFields();
        }
    }

    private void ResetFields() {
        m_JitterDuration = 0;
        m_Jitter = false;
        transform.localPosition = startingLocalPosition;
    }

    public void DoJitter(float jitterDurationElement, float jitterFactor) {
        if (m_Jitter) {
            // ResetFields();
        } else {
            //Debug.Log("Sart jittering");
            m_Jitter = true;
            m_JitterDuration = jitterDurationElement;
            m_JitterRange = jitterFactor;
        }
    }
}
