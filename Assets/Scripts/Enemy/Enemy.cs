﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Note: Object must be spawned facing to the Right!

[RequireComponent(typeof(EnemyHealth))]
public class Enemy : MonoBehaviour
{


    [SerializeField] protected AudioClip attackSound;
    [SerializeField] [Range(1, 50)] protected float awarenessRadius = 10;
    [SerializeField] [Range(1, 50)] protected float attackRadius = 5;
    [SerializeField] [Range(30, 360)] public float visionAngle = 100;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] protected float timeBetweenAttacks = 2f;
    [SerializeField] protected float movementSpeed = 2;

    [HideInInspector] public bool m_Attacking = false;
    protected float timeSinceLastAttack = 0;
    protected bool m_CanAttack = true;

    private bool m_Aware = false;
    private bool m_InAttackRange = false;

    // Components
    GrappleHookDJ playerGrappleScript;
    protected Animator m_Anim;
    protected Rigidbody2D rb;
    protected AudioSource audioSource;
    protected EnemyAI enemyAI;

    public virtual void Awake() {
        enemyAI = GetComponent<EnemyAI>();
        audioSource = GetComponent<AudioSource>();
        m_Anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerGrappleScript = GameManager.Player.GetComponent<GrappleHookDJ>();
    }

    private void Update() {
        if (Dead) return;
        Debug.Assert(rb != null);

        timeSinceLastAttack += Time.deltaTime;

        if (timeBetweenAttacks < timeSinceLastAttack &&
            InAttackRange && Aware && !PlayerIsDead && !GrappledByPlayer) {
            this.Attack();
            timeSinceLastAttack = 0;
        }
        FaceAimDirection();
    }

    private void FixedUpdate() {
        if (!Dead && Aware)
            MoveToTarget(PlayerPosLeveled);
    }

    private void LateUpdate() {
        UpdateAnimParams();
    }

    public virtual void Attack() {
        if (!m_CanAttack) return;
        m_Attacking = true;
        m_Anim.SetTrigger("Attack");
        m_Attacking = false;
    }

    public void OnGrapple() {
        //TODO: make IGrappleable interface
        timeSinceLastAttack = 0;
    }

    private void MoveToTarget(Vector3 targetPos) {
        if (enemyAI.isActiveAndEnabled && enemyAI != null)
            enemyAI.MoveAlongPath();
    }

    private void FaceAimDirection() {
        float aimDir = this.rb.velocity.x;
        if (m_Aware) aimDir = ToPlayerVector.x;

        // If moving right, and facing left, Flip()
        if (aimDir > 0 && transform.localScale.x < 0) Flip();
        // If moving left and facing right, Flip()
        if (aimDir < 0 && transform.localScale.x > 0) Flip();
    }
    private void Flip() {
        Vector3 theScale = transform.localScale;
        theScale.x = -1 * (theScale.x);
        transform.localScale = theScale;

        //Flip healthbar
        var healthBar = GetComponent<EnemyHealth>().healthBar;
        Vector3 healthBarScale = healthBar.transform.localScale;
        healthBarScale.x = -1 * (healthBarScale.x);
        healthBar.transform.localScale = healthBarScale;
    }

    private void UpdateAnimParams() {
        m_Anim.SetFloat("HSpeed", Mathf.Abs(transform.InverseTransformDirection(rb.velocity).x));
    }


    // Properties
    private int FacingSign { get { return (int)Mathf.Sign(transform.localScale.x); } }

    private bool InAttackRange {
        get {
            m_InAttackRange = false;
            float angle = Vector3.Angle(transform.right * FacingSign, ToPlayerVector);

            //Checks distance and angle
            if (ToPlayerVector.magnitude < attackRadius && Mathf.Abs(angle) < visionAngle)
                m_InAttackRange = true;
            return m_InAttackRange;

        }
        set { m_InAttackRange = value; }
    }

    public bool GrappledByPlayer {
        get {
            return playerGrappleScript != null && gameObject == playerGrappleScript.grabbedObj;
        }
    }

    /// <summary>
    /// returns player position with the same Y component as this enemy
    /// </summary>
    public Vector2 PlayerPosLeveled {
        get { return new Vector2(GameManager.Player.transform.position.x, transform.position.y); }
    }
    /// <summary>
    /// Vector to the player position
    /// </summary>
    private Vector2 ToPlayerVector { get { return GameManager.Player.transform.position - this.transform.position; } }

    public bool Aware {
        get {
            m_Aware = GameManager.Player && ToPlayerVector.magnitude < awarenessRadius;
            return m_Aware;
        }
        set { m_Aware = value; }
    }
    public bool Dead { get { return GetComponent<HealthScript>().isDead; } }
    public bool PlayerIsDead { get { return GameManager.Player && GameManager.Player.GetComponent<HealthScript>().isDead; } }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, awarenessRadius);

        Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * FacingSign * attackRadius + Vector3.up * attackRadius * Mathf.Tan(visionAngle * Mathf.Deg2Rad / 2));
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * FacingSign * attackRadius - Vector3.up * attackRadius * Mathf.Tan(visionAngle * Mathf.Deg2Rad / 2));
    }
}