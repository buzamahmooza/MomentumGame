using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Note: Object must be spawned facing to the Right!
[RequireComponent(typeof(Health))]
public class Enemy : Walker
{
    [SerializeField] protected AudioClip attackSound;
    [SerializeField] [Range(1, 50)] protected float awarenessRadius = 10;
    [SerializeField] [Range(1, 50)] protected float attackRadius = 5;
    [SerializeField] [Range(30, 360)] public float visionAngle = 100;
    [SerializeField] protected float timeBetweenAttacks = 2f;
    [SerializeField] protected float movementSpeed = 2;

    [HideInInspector] public bool m_Attacking = false;
    protected float timeSinceLastAttack = 0;
    protected bool m_CanAttack = true;

    private bool m_Aware = false;
    private bool m_InAttackRange = false;

    // Components
    GrappleHookDJ playerGrappleScript;
    protected EnemyAI enemyAI;

    protected override void Awake() {
        base.Awake();
        enemyAI = GetComponent<EnemyAI>();
        playerGrappleScript = GameManager.Player.GetComponent<GrappleHookDJ>();

        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }

    protected void Start() {
        if (!targeting.Target)
            targeting.Target = GameManager.Player.transform;
    }

    protected void Update() {
        if (health.IsDead) return;
        Debug.Assert(rb != null);

        timeSinceLastAttack += Time.deltaTime;

        if (IsAware) {
            FaceDirection(targeting.Target.transform.position.x - transform.position.x);

            if (timeBetweenAttacks < timeSinceLastAttack && InAttackRange && !TargetIsDead && !IsGrappled) {
                this.Attack();
                timeSinceLastAttack = 0;
            }
        }

        FaceAimDirection();
    }

    private void FixedUpdate() {
        if (!health.IsDead && IsAware) {
            MoveToTarget(TargetPosLeveled);
            FaceAimDirection();
        }
    }

    private void LateUpdate() {
        UpdateAnimParams();
    }

    public virtual void Attack() {
        if (!m_CanAttack) return;

        m_Attacking = true;
        _anim.SetTrigger("Attack");
    }

    public void OnGrapple() {
        //TODO: make IGrappleable interface
        timeSinceLastAttack = 0;
    }

    private void MoveToTarget(Vector3 targetPos) {
        if (enemyAI.isActiveAndEnabled && enemyAI != null)
            enemyAI.MoveAlongPath();
    }

    private void UpdateAnimParams() {
        _anim.SetFloat("HSpeed", Mathf.Abs(rb.velocity.x));
    }


    private bool InAttackRange {
        get {
            m_InAttackRange = false;
            float angle = Vector3.Angle(transform.right * FacingSign, targeting.AimDirection);

            //Checks distance and angle
            if (targeting.AimDirection.magnitude < attackRadius && Mathf.Abs(angle) < visionAngle)
                m_InAttackRange = true;
            return m_InAttackRange;

        }
        set { m_InAttackRange = value; }
    }
    // TODO: replace this property with a bool field
    public bool IsGrappled { get { return playerGrappleScript != null && gameObject == playerGrappleScript.grabbedObj; } }

    /// <summary>
    /// returns player position with the same Y component as this enemy
    /// </summary>
    protected Vector2 TargetPosLeveled {
        get { return new Vector2(targeting.Target.transform.position.x, transform.position.y); }
    }

    public bool IsAware {
        get {
            m_Aware = targeting.Target && targeting.AimDirection.magnitude < awarenessRadius;
            return m_Aware;
        }
        set { m_Aware = value; }
    }
    public bool TargetIsDead { get { return targeting.Target && targeting.Target.GetComponent<Health>().IsDead; } }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, awarenessRadius);

        Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * FacingSign * attackRadius + Vector3.up * attackRadius * Mathf.Tan(visionAngle * Mathf.Deg2Rad / 2));
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * FacingSign * attackRadius - Vector3.up * attackRadius * Mathf.Tan(visionAngle * Mathf.Deg2Rad / 2));
    }
}
