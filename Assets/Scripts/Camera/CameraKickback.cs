using UnityEngine;

public class CameraKickback : MonoBehaviour
{
    [SerializeField] float DefaultKickbackDuration = 0.2f;
    [SerializeField] [Range(0, 1)] float smooth = 0.3f;
    [SerializeField] float m_KickbackDuration = 0.2f; //

    private Vector3 m_TargetPos,
        m_KickbackDirection = Vector3.zero;

    private bool m_kickback = false;
    private Vector3 m_startingLocalPosition;

    private void Start()
    {
        m_startingLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (m_kickback)
        {
            Kickback();
        }
        else
        {
            m_KickbackDuration = DefaultKickbackDuration;
        }
    }

    // Update is called once per frame
    private void Kickback()
    {
        // Create a target position to aim for
        m_TargetPos = m_KickbackDirection;
        // Linearly interpolate to this target position
        transform.localPosition = Vector3.Lerp(transform.localPosition, m_TargetPos, smooth / m_KickbackDuration);

        // Decrease KickbackFactor over time
        m_KickbackDuration -= Time.deltaTime;
        m_KickbackDirection = m_KickbackDirection * m_KickbackDuration;

        // Make sure KickbackFactor reaches zero

        if (m_KickbackDuration <= 0.0f)
        {
            ResetFields();
        }
    }

    private void ResetFields()
    {
        m_kickback = false;
        m_KickbackDuration = 0;
        m_KickbackDirection = Vector3.zero;
        m_TargetPos = m_startingLocalPosition;
    }

    public void DoKickback(Vector3 kickbackDir)
    {
        if (m_kickback)
        {
            //ResetFields();
        }
        else
        {
            m_kickback = true;

            // Random vector to add to the current position   
            m_KickbackDirection = kickbackDir;


            m_TargetPos += kickbackDir;
        }
    }
}
