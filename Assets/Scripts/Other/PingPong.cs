using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class PingPong : MonoBehaviour
{
    [FormerlySerializedAs("nodes")] [SerializeField]
    private Transform[] m_nodes = new Transform[2];

    [SerializeField] [Range(0, 5)] private float m_speed = 0.5f;

    // how many seconds to wait at each node when reached
    [SerializeField] [Header("how many seconds to wait at each node when reached")] [Range(0, 5)]
    private float m_stopTimeOnNodes = 0.5f; //TODO: not yet implemented

    /// <summary>
    /// if set to true, objects colliding will become children
    /// (they'll get stuck and will move with the platform)
    /// </summary>
    [SerializeField]
    [Header("objects colliding will become children (they'll get stuck and will move with the platform)")]
    private bool m_isPlatform = true;

    private bool m_incrementingUp = true;
    private float m_t = 0;

    /// the current node that the object is chasing
    private int m_current;

    private bool m_moving = true;


    private float DistanceToTarget => Vector3.Distance(transform.position, m_nodes[m_current].position);

    void Start()
    {
        m_nodes = m_nodes.Where(node => node != null).ToArray();
        m_current = 0;
    }


    private void LateUpdate()
    {
        m_t += Time.deltaTime;

        ApproachTarget();
    }

    private void ApproachTarget()
    {
        if (!m_moving) return;

        var distanceToTarget = DistanceToTarget;

        transform.position = Vector3.Lerp(
            transform.position,
            m_nodes[m_current].position,
            m_t * m_speed * Time.deltaTime / (distanceToTarget + 1)
        );


        if (distanceToTarget <= 0.1f) // when target reached
        {
            m_t = 0;
            m_moving = false;
            Invoke(nameof(IncrementTarget), m_stopTimeOnNodes);
        }
    }

    private void IncrementTarget()
    {
        m_moving = true;
        // if reached the end, reverse
        if (m_current >= m_nodes.Length - 1)
        {
            m_incrementingUp = false;
        }
        else if (m_current <= 0)
        {
            m_incrementingUp = true;
        }


        if (m_incrementingUp)
        {
            m_current++;
        }
        else
        {
            m_current--;
        }
    }


    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!m_isPlatform) return;
        other.transform.parent = this.transform;
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (!m_isPlatform) return;
        other.transform.parent = null;
    }

    private void OnDrawGizmos()
    {
        // draw a point on each node
        foreach (Transform node in m_nodes)
            Gizmos.DrawSphere(node.transform.position, radius: 0.1f);

        // connect the dots with lines
        for (int i = 0; i < m_nodes.Length - 1; i++)
            Gizmos.DrawLine(m_nodes[i].transform.position, m_nodes[i + 1].transform.position);
    }

// makes it animate the inspector
#if UNITY_EDITOR
    private void OnValidate()
    {
        InvokeRepeating(nameof(FakeUpdate), 0, Time.deltaTime);
    }

    private void FakeUpdate()
    {
        m_t += Time.deltaTime;
        ApproachTarget();
    }
#endif
}