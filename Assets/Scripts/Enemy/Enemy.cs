using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Note: Object must be spawned facing to the Right!
[RequireComponent(typeof(EnemyHealth))]
public class Enemy : Walker
{
    [SerializeField] protected AudioClip attackSound;
    [SerializeField] [Range(1, 50)] protected float awarenessRadius = 10;
    [SerializeField] [Range(1, 50)] protected float attackRange = 5;
    [SerializeField] [Range(30, 360)] public float visionAngle = 100;
    [SerializeField] [Range(0, 100)] protected float timeBetweenAttacks = 2f; // in seconds

    [HideInInspector]
    public bool m_Attacking = false;
    protected float timeSinceLastAttack = 0;
    protected bool m_CanAttack = true;

    protected bool m_Aware = false;
    protected bool m_InAttackRange = false;

    // Components
    GrappleHookDJ playerGrappleScript;
    protected EnemyAI enemyAI;


    protected override void Awake()
    {
        base.Awake();
        enemyAI = GetComponent<EnemyAI>();
        playerGrappleScript = GameManager.Player.GetComponent<GrappleHookDJ>();
        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }

    protected virtual void Start()
    {
        if (!targeting.Target)
            targeting.Target = GameManager.Player.transform;
    }

    protected virtual void Update()
    {
        if (health.IsDead) return;
        Debug.Assert(rb != null);

        timeSinceLastAttack += Time.deltaTime;

        if (IsAware)
        {
            FaceDirection(targeting.Target.transform.position.x - transform.position.x);

            if (timeBetweenAttacks < timeSinceLastAttack && InAttackRange && !TargetIsDead && !IsGrappled)
            {
                this.Attack();
                timeSinceLastAttack = 0;
            }
        }

        FaceAimDirection();
    }
    protected virtual void FixedUpdate()
    {
        if (!health.IsDead && IsAware)
        {
            print(gameObject.name + " moving toward target: " + targeting.Target.name);
            MoveToTarget(TargetPosLeveled);
            FaceAimDirection();
        }
    }
    protected virtual void LateUpdate()
    {
        UpdateAnimParams();
    }


    public virtual void Attack()
    {
        if (!m_CanAttack) return;
        if (attackSound) audioSource.PlayOneShot(attackSound);
        m_Attacking = true;
        _anim.SetTrigger("Attack");
    }

    private void MoveToTarget(Vector3 targetPos)
    {
        if (enemyAI != null && enemyAI.isActiveAndEnabled)
        {
            UpdateAnimatorParams();
            enemyAI.MoveAlongPath();
        }
    }

    private void UpdateAnimParams()
    {
        _anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }


    public bool InAttackRange
    {
        get
        {
            m_InAttackRange = false;
            float angle = Vector3.Angle(transform.right * FacingSign, targeting.AimDirection);

            //Checks distance and angle
            m_InAttackRange = targeting.AimDirection.magnitude < attackRange && Mathf.Abs(angle) < visionAngle || targeting.AimDirection.magnitude < (attackRange * 0.4f); // the small circlular raduis
            return m_InAttackRange;
        }
        set { m_InAttackRange = value; }
    }

    public bool IsGrappled { get { return playerGrappleScript != null && gameObject == playerGrappleScript.grabbedObj; } }

    /// <summary>
    /// returns player position with the same Y component as this enemy
    /// </summary>
    protected Vector2 TargetPosLeveled
    {
        get { return new Vector2(targeting.Target.transform.position.x, transform.position.y); }
    }

    public bool IsAware
    {
        get
        {
            m_Aware = targeting.Target && targeting.AimDirection.magnitude < awarenessRadius;
            return m_Aware;
        }
        set { m_Aware = value; }
    }
    public bool TargetIsDead { get { return targeting.Target && targeting.Target.GetComponent<Health>().IsDead; } }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, awarenessRadius);

        Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.AngleAxis(visionAngle / 2, Vector3.back) * Vector3.right * FacingSign * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.AngleAxis(visionAngle / 2, Vector3.forward) * Vector3.right * FacingSign * attackRange);
        Gizmos.DrawWireSphere(transform.position, (attackRange * 0.4f)); // the small circlular raduis
    }
}
