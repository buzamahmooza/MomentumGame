using UnityEngine;
using System.Collections;
//Note this line, if it is left out, the script won't know that the class 'Path' exists and it will throw compiler errors
//This line should always be present at the top of scripts which use pathfinding
using Pathfinding;
using System;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Seeker))]
public class EnemyAI : MonoBehaviour
{
    //The point to move to
    private Vector3 targetPosition;

    public Transform target;
    public float updateDelay = 1f;

    private Seeker seeker;
    private Rigidbody2D rb;

    //The calculated path
    public Path path;

    //The AI's speed per second
    public float speed = 100;
    public ForceMode2D fMode;
    public bool useVelocity = false;

    private bool pathIsEnded = false;

    //The max distance from the AI to a waypoint for it to continue to the next waypoint
    public float nextWaypointDistance = 3;

    //The waypoint we are currently moving towards
    private int currentWaypoint = 0;

    [SerializeField] private float stopDist = 2f;
    public LayerMask mask;

    public bool inSight = false;
    public bool followOnGround;
    private Enemy enemyScript;
    private Animator anim;

    private bool m_Grounded;
    public LayerMask floorMask;
    public Transform m_GroundCheck;
    private float k_GroundedRadius = .2f;
    public float heightToJump = 1;
    public float angleToJump = 45;

    private float nextPointAngle;
    private Vector3 moveDirection;

    private void Awake() {
        anim = GetComponent<Animator>();
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        enemyScript = GetComponent<Enemy>();
        m_GroundCheck = transform.Find("GroundCheck");
    }

    public void Start() {
        if (!TargetExists()) return;

        targetPosition = target.position;
        //stopDist = enemyScript.awarenessRadius * 0.75f;
        //Start a new path to the targetPosition, return the result to the OnPathComplete function
        seeker.StartPath(transform.position, targetPosition, OnPathComplete);

        StartCoroutine("UpdatePath");
    }

    public void MoveAlongPath() {
        if (!enemyScript.m_Attacking && !enemyScript.Dead) {
            ApproachTarget();
        }
    }

    public bool CalculateWaypoint() {
        if (target == null) {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }
        if (target == null) return false;
        targetPosition = target.position;


        //We have no path to move after yet
        if (path == null) return false;

        // If path ended
        if (currentWaypoint >= path.vectorPath.Count) {
            if (pathIsEnded) return false;
            Debug.Log("End Of Path Reached");
            rb.velocity = new Vector2(0, rb.velocity.y);

            pathIsEnded = true;
            return false;
        }
        pathIsEnded = false;
        inSight = false;

        // If not yet reached
        if ((stopDist > Vector3.Distance(transform.position, target.position) && TargetExists()))
            return false;

        if (anim != null) anim.SetBool("Walk", true);
        Vector3 nextWaypoint = path.vectorPath[currentWaypoint] - transform.position;

        //Check if we are close enough to the next waypoint. If we are, proceed to follow the next waypoint
        if (nextWaypoint.magnitude < nextWaypointDistance) currentWaypoint++;

        //Direction to the next waypoint
        Vector2 dir = nextWaypoint;
        if (followOnGround) dir = new Vector3(dir.x, -rb.gravityScale);
        moveDirection = dir.normalized;
        return true;
    }

    private void ApproachTarget() {
        bool result = CalculateWaypoint();
        if (!result) { // if result is false, just stop moving
            moveDirection = Vector3.zero;
            if (anim != null) anim.SetBool("Walk", false);
            return;
        }

        Vector3 dir = moveDirection * speed * Time.fixedDeltaTime;

        Debug.DrawLine(transform.position, transform.position + dir, Color.yellow);
        Debug.DrawLine(transform.position, target.position, Color.red);

        if (useVelocity) rb.velocity = dir;
        else rb.AddForce(dir * 10, fMode);

        // Jump
        //nextPointAngle = Vector3.Angle(dir.normalized, Vector3.up);
        //if (nextPointAngle < angleToJump && Grounded) {
        //}
    }

    private IEnumerator UpdatePath() {
        if (TargetExists()) {
            targetPosition = target.position;

            //Start a new path to the targetPosition, return the result to the OnPathComplete function
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);

            if (updateDelay != 0) yield return new WaitForSeconds(updateDelay);
            StartCoroutine("UpdatePath");
        }
    }

    public void OnPathComplete(Path p) {
        if (!p.error) {
            path = p;
            currentWaypoint = 0; //Reset the waypoint counter
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = enemyScript && enemyScript.Aware ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, stopDist);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns>returns true if there is a target</returns>
    private bool TargetExists() {
        if (target == null) {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                target = go.transform;
        }
        if (target == null) {
            Debug.Log("Target is null");
            return false;
        }
        return true;
    }

    public bool Grounded {
        get {
            // If player is not moving vertically
            //if (Mathf.Abs(rb.velocity.y) < 0.1) {
            //	m_Grounded = true;
            //	return m_Grounded;
            //}
            Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, floorMask);

            m_Grounded = false;
            foreach (Collider2D col in colliders) {
                if (col.gameObject == gameObject) break;
                else m_Grounded = true;
            }
            return m_Grounded;
        }
        set { m_Grounded = value; }
    }
}