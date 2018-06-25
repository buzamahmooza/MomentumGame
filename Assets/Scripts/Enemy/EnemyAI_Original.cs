using UnityEngine;
using System.Collections;
//Note this line, if it is left out, the script won't know that the class 'Path' exists and it will throw compiler errors
//This line should always be present at the top of scripts which use pathfinding
using Pathfinding;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Seeker))]
public class EnemyAI_Original : MonoBehaviour
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

    public float stopDist = 2f;

    private Animator anim;
    public bool lookAtPlayer;
    public bool moveHorizontally = true;
    private GameObject player;
    private Enemy enemyScript;

    public void Awake()
    {
        enemyScript = GetComponent<Enemy>();
        anim = GetComponent<Animator>();

        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Start()
    {
        if (target == null) {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target == null) return;
        targetPosition = target.position;

        //Start a new path to the targetPosition, return the result to the OnPathComplete function
        seeker.StartPath(transform.position, targetPosition, OnPathComplete);

        StartCoroutine("UpdatePath");
    }

    private IEnumerator UpdatePath()
    {
        if (target == null) {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            yield break;
        }
        targetPosition = target.position;

        //Start a new path to the targetPosition, return the result to the OnPathComplete function
        seeker.StartPath(transform.position, targetPosition, OnPathComplete);

        yield return new WaitForSeconds(updateDelay);
        StartCoroutine("UpdatePath");
    }

    public void OnPathComplete(Path p)
    {
        if (!p.error) {
            path = p; currentWaypoint = 0;
        }
    }

    public void MoveAlongPath()
    {
        //We have no path to move after yet
        if (path == null) return;

        if (target == null) {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            return;
        }
        targetPosition = target.position;

        if (anim != null)
            anim.SetBool("Walk", false);
        if (useVelocity)
            rb.velocity = Vector2.zero;

        //Look at the player
        if (lookAtPlayer) {
            Vector3 pos = target.position;
            Vector3 lookDir = pos - transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }

        if (currentWaypoint >= path.vectorPath.Count) {
            if (pathIsEnded) return;

            pathIsEnded = true;
            return;
        }
        pathIsEnded = false;

        if (stopDist > Vector3.Distance(transform.position, target.position) || player) {
            //Direction to target
            Vector3 targetDir = (target.position - transform.position).normalized;
            return;
        }

        //Direction to the next waypoint
        Vector3 moveDir = (path.vectorPath[currentWaypoint] - transform.position).normalized;

        moveDir *= speed * Time.fixedDeltaTime;
        if (moveHorizontally)
            moveDir = new Vector3(moveDir.x, -2, moveDir.z);

        if (useVelocity) rb.velocity = moveDir; else rb.AddForce(moveDir, fMode);

        if (anim != null)
            anim.SetBool("Walk", true);

        //Check if we are close enough to the next waypoint
        //If we are, proceed to follow the next waypoint
        if (Vector3.Distance(transform.position, path.vectorPath[currentWaypoint]) < nextWaypointDistance)
            currentWaypoint++;
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyScript != null && enemyScript.Aware)
            Gizmos.color = Color.red;
        else Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(transform.position, stopDist);
    }
}