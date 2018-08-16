using System;
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

    [HideInInspector] public bool IsAttacking = false;
    protected float TimeSinceLastAttack = -float.NegativeInfinity;
    protected bool CanAttack = true;

    // Components
    private GrappleHook m_grapple;
    protected EnemyAI Ai;

    /// <summary>
    /// Never override Awake without invoking base.Awake()
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Ai = GetComponent<EnemyAI>();
        m_grapple = GameManager.Player.GetComponent<GrappleHook>();
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (!Targeting.Target)
            Targeting.Target = GameManager.Player.transform;
    }

    protected virtual void Start()
    {
        CanAttack = false;
        // wait a random delay until allowed to attack
        Invoke("AllowAttack", Random.Range(0.5f, 2));
    }

    protected virtual void Update()
    {
        if (Health.IsDead)
            return;
        Debug.Assert(Rb != null);

        TimeSinceLastAttack += Time.deltaTime;
    }

    protected virtual void FixedUpdate()
    {
        if (!Health.IsDead && IsAware)
        {
            Anim.SetTrigger("Aware");
            FaceDirection(Targeting.Target.transform.position.x - transform.position.x);

            if (timeBetweenAttacks < TimeSinceLastAttack && IsInAttackRange && !TargetIsDead && !IsGrappled)
            {
                Attack();
                TimeSinceLastAttack = 0;
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
        CanAttack = true;
    }


    public virtual void Attack()
    {
        if (!CanAttack)
        {
            Invoke("Attack", 0.5f);
            return;
        }

        if (attackSound)
            AudioSource.PlayOneShot(attackSound);

        IsAttacking = true;
        Anim.SetTrigger("Attack");
    }

    private void MoveToTarget()
    {
        if (Ai != null && Ai.enabled)
        {
            UpdateAnimatorParams();
            Ai.MoveAlongPath();
        }
    }

    private void UpdateAnimParams()
    {
        Anim.SetFloat("Speed", Mathf.Abs(Rb.velocity.x));
    }


    public bool IsInAttackRange
    {
        get
        {
            float angle = Vector3.Angle(transform.right * FacingSign, Targeting.AimDirection);

            //Checks distance and angle
            return Targeting.AimDirection.magnitude < attackRange && Mathf.Abs(angle) < visionAngle ||
                              Targeting.AimDirection.magnitude < (attackRange * 0.4f); // the small circlular raduis
        }
    }

    public bool IsGrappled
    {
        get { return m_grapple != null && gameObject == m_grapple.GrabbedObj; }
    }

    public bool IsAware
    {
        get { return Targeting.Target && Targeting.AimDirection.magnitude < awarenessRadius; }
    }

    public bool TargetIsDead
    {
        get { return Targeting.Target && Targeting.Target.GetComponent<Health>().IsDead; }
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