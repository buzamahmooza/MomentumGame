using UnityEngine;
using System.Collections;
//Note this line, if it is left out, the script won't know that the class 'Path' exists and it will throw compiler errors
//This line should always be present at the top of scripts which use pathfinding
using Pathfinding;
using System;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(Enemy))]
public class EnemyAI : Targeting
{
    //The point to move to
    private Vector3 targetPosition;

    public float updateDelay = 1f;

    private Seeker seeker;
    private Rigidbody2D rb;

    //The calculated path
    public Path path;

    //The AI's speed per second

    private bool pathIsEnded = false;

    //The max distance from the AI to a waypoint for it to continue to the next waypoint
    public float nextWaypointDistance = 3;

    //The waypoint we are currently moving towards
    private int currentWaypoint = 0;

    [SerializeField] private float stopDist = 2f;

    public bool followOnGround;
    private Enemy enemyScript;
    private Targeting targeting;
    private Animator anim;

    private bool m_Grounded;
    public LayerMask floorMask;
    public Transform m_GroundCheck;
    private float k_GroundedRadius = .2f;

    // experimental
    public float heightToJump = 1;
    public float angleToJump = 45;

    private Vector3 moveDirection;

    protected virtual void Awake() {
        targeting = GetComponent<Targeting>();
        anim = GetComponent<Animator>();
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        enemyScript = GetComponent<Enemy>();
        m_GroundCheck = transform.Find("GroundCheck");
    }

    protected virtual void Start() {
        if (!TargetExists()) return;

        targetPosition = Target.position;
        //stopDist = enemyScript.awarenessRadius * 0.75f;
        //Start a new path to the targetPosition, return the result to the OnPathComplete function
        seeker.StartPath(transform.position, targetPosition, OnPathComplete);

        StartCoroutine("UpdatePath");
    }

    public void MoveAlongPath() {
        if (!enemyScript.m_Attacking && !enemyScript.health.IsDead) {
            ApproachTarget();
        }
    }

    public bool CalculateWaypoint() {
        CheckTarget();
        if (Target == null) return false;
        targetPosition = Target.position;


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

        // If not yet reached
        if ((stopDist > Vector3.Distance(transform.position, Target.position) && TargetExists()))
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

    private void CheckTarget() {
        Transform tr = targeting.Target;
        if (tr != null) Target = tr.transform;

        //GameObject go = GameObject.FindGameObjectWithTag("Player");
        //if (go != null) Target = go.transform;
    }

    private void ApproachTarget() {
        bool result = CalculateWaypoint();
        if (!result) { // if result is false, just stop moving
            moveDirection = Vector3.zero;
            if (anim != null) anim.SetBool("Walk", false);
            return;
        }

        Vector3 dir = moveDirection * Time.fixedDeltaTime;

        Debug.DrawLine(transform.position, transform.position + dir, Color.yellow);
        Debug.DrawLine(transform.position, Target.position, Color.red);

        enemyScript.Move(dir, false);

        // Jump
        //nextPointAngle = Vector3.Angle(dir.normalized, Vector3.up);
        //if (nextPointAngle < angleToJump && Grounded) {
        //}
    }

    private IEnumerator UpdatePath() {
        if (TargetExists()) {
            targetPosition = Target.position;

            //Start a new path to the targetPosition, return the result to the OnPathComplete function
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);

            if (updateDelay > 0)
                yield return new WaitForSeconds(updateDelay);
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
        Gizmos.color = enemyScript && enemyScript.IsAware ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, stopDist);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns>returns true if there is a Target</returns>
    private bool TargetExists() {
        CheckTarget();
        if (Target == null) {
            Debug.Log("Target is null");
            return false;
        }
        return true;
    }

    public bool Grounded {
        get {
            m_Grounded = false;
            foreach (Collider2D col in Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, floorMask)) {
                if (col.gameObject == gameObject) break;
                else m_Grounded = true;
            }
            return m_Grounded;
        }
        set { m_Grounded = value; }
    }
}