﻿using UnityEngine;
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

    //The calculated path
    public Path path;

    [SerializeField] private float stopDist = 2f;
    [SerializeField] public bool followOnGround = true;
    [SerializeField] private int _debugLevel = 0;

    public LayerMask floorMask;
    public Transform m_GroundCheck;

    //The max distance from the AI to a waypoint for it to continue to the next waypoint
    public float nextWaypointDistance = 3;

    // experimental
    [SerializeField] private float heightToJump = 1;
    [SerializeField] private float angleToJump = 45;

    private bool pathHasEnded = false;

    //The waypoint we are currently moving towards
    private int currentWaypoint = 0;
    private readonly float k_GroundedRadius = .2f;

    private Vector3 moveDirection;

    private Seeker seeker;
    private Rigidbody2D rb;
    private Enemy enemyScript;
    private Targeting targeting;
    private Animator anim;


    protected virtual void Awake()
    {
        targeting = GetComponent<Targeting>();
        anim = GetComponent<Animator>();
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        enemyScript = GetComponent<Enemy>();
        m_GroundCheck = transform.Find("GroundCheck");
    }

    protected virtual void Start()
    {
        if (!TargetExists()) return;

        targetPosition = Target.position;
        //stopDist = enemyScript.awarenessRadius * 0.75f;
        //Start a new path to the targetPosition, return the result to the OnPathComplete function
        seeker.StartPath(transform.position, targetPosition, OnPathComplete);

        StartCoroutine(UpdatePathWithDelayRecursive());
    }


    /// <summary>
    /// if (!m_Attacking && !isDead) ApproachTarget();
    /// </summary>
    public void MoveAlongPath()
    {
        if (!enemyScript.m_Attacking && !enemyScript.health.IsDead)
        {
            ApproachTarget();
        }
    }

    private void ApproachTarget()
    {
        bool result = CalculateWaypoint();
        if (!result)
        {
            // if result is false, just stop moving

            if (_debugLevel > 2)
                Debug.Log("CalculateWaypoint result is FALSE :(");
            moveDirection = Vector3.zero;
            if (anim) anim.SetBool("Walk", false);
            return;
        }

        Vector3 dir = moveDirection * Time.fixedDeltaTime;

        Debug.DrawLine(transform.position, transform.position + dir, Color.yellow);
        Debug.DrawLine(transform.position, Target.position, Color.red);

        enemyScript.Move(dir);

        // Jump
        //nextPointAngle = Vector3.Angle(dir.normalized, Vector3.up);
        //if (nextPointAngle < angleToJump && Grounded) {}
    }

    /// <summary>
    /// Returns false if target is null or too close
    /// TODO: needs performance improvement! This method currently takes up 14% of the CPU process time
    /// </summary>
    /// <returns></returns>
    public bool CalculateWaypoint()
    {
        if (!TargetExists())
        {
            if (_debugLevel >= 1)
                Debug.LogWarning(name + " Target is null");
            return false;
        }

        targetPosition = Target.position;


        //We have no path to move after yet
        if (path == null)
        {
            if (_debugLevel >= 1)
                Debug.LogWarning(name + " path == null");
            UpdatePath();
            return false;
        }

        // If path ended
        if (currentWaypoint >= path.vectorPath.Count)
        {
            if (_debugLevel >= 2)
                Debug.Log("End Of Path Reached");
            if (pathHasEnded)
                return false;
            rb.velocity = new Vector2(0, rb.velocity.y);

            pathHasEnded = true;
            return false;
        }

        pathHasEnded = false;

        // If too close
        if (stopDist > Vector3.Distance(transform.position, Target.position) && TargetExists())
        {
            return false;
        }

        if (anim != null) anim.SetBool("Walk", true);
        Vector3 nextWaypoint = path.vectorPath[currentWaypoint] - transform.position;

        //Check if we are close enough to the next waypoint. If we are, proceed to follow the next waypoint
        if (nextWaypoint.magnitude < nextWaypointDistance) currentWaypoint++;

        //Direction to the next waypoint
        Vector2 dir = nextWaypoint;
        if (followOnGround) 
            dir = new Vector3(dir.x, -rb.gravityScale);
        moveDirection = dir;
        return true;
    }

    private IEnumerator UpdatePathWithDelayRecursive()
    {
        if (TargetExists())
        {
            UpdatePath();

            if (updateDelay > 0) yield return new WaitForSeconds(updateDelay);
            StartCoroutine(UpdatePathWithDelayRecursive());
        }
    }

    private void UpdatePath()
    {
        if (TargetExists())
        {
            targetPosition = Target.position;

            //Start a new path to the targetPosition, return the result to the OnPathComplete function
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);
        }
    }

    public void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0; //Reset the waypoint counter
        }
    }

    /// <summary>
    /// returns true if there is a Target
    /// </summary>
    /// <returns>returns true if there is a Target</returns>
    private bool TargetExists()
    {
        Transform tr = targeting.Target;
        if (tr != null) Target = tr.transform;

        //GameObject go = GameObject.FindGameObjectWithTag("Player");
        //if (go != null) Target = go.transform;

        if (Target == null)
        {
            if (_debugLevel >= 1)
                Debug.Log("Target is null");
            return false;
        }

        return true;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = enemyScript && enemyScript.IsAware ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, stopDist);
    }
}