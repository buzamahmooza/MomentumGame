﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Note: Object must be spawned facing to the Right!
[RequireComponent(typeof(EnemyHealth))]
public class Enemy : Walker
{
    [SerializeField] protected AudioClip attackSound;
    [SerializeField] [Range(1, 50)] protected float awarenessRadius = 10;
    [SerializeField] [Range(1, 50)] protected float attackRange = 5;
    [SerializeField] [Range(30, 360)] public float visionAngle = 100;
    [SerializeField] [Range(0, 100)] protected float timeBetweenAttacks = 2f; // in seconds

    [HideInInspector] public bool m_Attacking = false;
    protected float timeSinceLastAttack = -float.NegativeInfinity;
    protected bool m_CanAttack = true;

    protected bool m_InAttackRange = false;

    // Components
    GrappleHook playerGrappleScript;
    protected EnemyAI ai;

    /// <summary>
    /// Never override Awake without invoking base.Awake()
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        ai = GetComponent<EnemyAI>();
        playerGrappleScript = GameManager.Player.GetComponent<GrappleHook>();
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (!targeting.Target)
            targeting.Target = GameManager.Player.transform;
    }

    protected virtual void Start()
    {
        m_CanAttack = false;
        // wait a random delay until allowed to attack
        Invoke("AllowAttack", Random.Range(0.5f, 2));
    }

    protected virtual void Update()
    {
        if (health.IsDead)
            return;
        Debug.Assert(Rb != null);

        timeSinceLastAttack += Time.deltaTime;
    }

    protected virtual void FixedUpdate()
    {
        if (!health.IsDead && IsAware)
        {
            _anim.SetTrigger("Aware");
            FaceDirection(targeting.Target.transform.position.x - transform.position.x);

            if (timeBetweenAttacks < timeSinceLastAttack && InAttackRange && !TargetIsDead && !IsGrappled)
            {
                Attack();
                timeSinceLastAttack = 0;
            }

            MoveToTarget();
            FaceAimDirection();
        }
    }

    protected virtual void LateUpdate()
    {
        UpdateAnimParams();
    }

    /// <summary>
    /// Sets canAttack to true,
    /// Only used in the beginning,
    /// allow the enemy to attack after a short time after spawn
    /// </summary>
    private void AllowAttack()
    {
        m_CanAttack = true;
    }


    public virtual void Attack()
    {
        if (!m_CanAttack)
        {
            Invoke("Attack", 0.5f);
            return;
        }

        if (attackSound)
            audioSource.PlayOneShot(attackSound);

        m_Attacking = true;
        _anim.SetTrigger("Attack");
    }

    private void MoveToTarget()
    {
        if (ai != null && ai.enabled)
        {
            UpdateAnimatorParams();
            ai.MoveAlongPath();
        }
    }

    private void UpdateAnimParams()
    {
        _anim.SetFloat("Speed", Mathf.Abs(Rb.velocity.x));
    }


    public bool InAttackRange
    {
        get
        {
            m_InAttackRange = false;
            float angle = Vector3.Angle(transform.right * FacingSign, targeting.AimDirection);

            //Checks distance and angle
            m_InAttackRange = targeting.AimDirection.magnitude < attackRange && Mathf.Abs(angle) < visionAngle ||
                              targeting.AimDirection.magnitude < (attackRange * 0.4f); // the small circlular raduis
            return m_InAttackRange;
        }
        set { m_InAttackRange = value; }
    }

    public bool IsGrappled
    {
        get { return playerGrappleScript != null && gameObject == playerGrappleScript.GrabbedObj; }
    }

    public bool IsAware
    {
        get { return targeting.Target && targeting.AimDirection.magnitude < awarenessRadius; }
    }

    public bool TargetIsDead
    {
        get { return targeting.Target && targeting.Target.GetComponent<Health>().IsDead; }
    }

    // used in animations
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, awarenessRadius);


        Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.AngleAxis(visionAngle / 2, Vector3.back) * Vector3.right * FacingSign *
            attackRange);
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.AngleAxis(visionAngle / 2, Vector3.forward) * Vector3.right * FacingSign *
            attackRange);
        Gizmos.DrawWireSphere(transform.position, (attackRange * 0.4f)); // the small circlular raduis
    }
}