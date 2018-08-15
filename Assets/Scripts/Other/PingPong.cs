using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class PingPong : MonoBehaviour
{
    [SerializeField] private Transform[] nodes = new Transform[2];
    [SerializeField] private float speed = 3;

    /// <summary>
    /// if set to true, objects colliding will become children
    /// (they'll get stuck and will move witht the platform)
    /// </summary>
    [SerializeField] private bool isPlatform = true;

    private int currentNode = 0;
    private bool incrementingUp = true;
    private float t = 0;

    /// <summary>
    /// the current node that the object is chasing 
    /// </summary>
    private int current;


    void Start()
    {
        nodes = nodes.Where(node => node != null).ToArray();
        current = 0;
    }

    private void LateUpdate()
    {
        float distance = Vector3.Distance(transform.position, nodes[current].position);
        transform.position = Vector3.Lerp(
            transform.position,
            nodes[current].position,
            t * speed * Time.deltaTime / distance
        );

        t += Time.deltaTime;

        if (distance <= 0.15f)
        {
            t = 0;
            IncrementTarget();
        }
    }

    private void IncrementTarget()
    {
        // if reached the end, reverse
        if (current >= nodes.Length - 1)
        {
            incrementingUp = false;
        }
        else if (current <= 0)
        {
            incrementingUp = true;
        }


        if (incrementingUp)
        {
            current++;
        }
        else
        {
            current--;
        }
    }


    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!isPlatform) return;
        other.transform.parent = this.transform;
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (!isPlatform) return;
        other.transform.parent = null;
    }

    private void OnDrawGizmos()
    {
        // connect the lines
        for (int i = 0; i < nodes.Length - 1; i++)
        {
            Gizmos.DrawLine(nodes[i].transform.position, nodes[i + 1].transform.position);
        }

        // draw points
        for (int i = 0; i < nodes.Length; i++)
            Gizmos.DrawSphere(nodes[i].transform.position, 0.1f);
    }
}