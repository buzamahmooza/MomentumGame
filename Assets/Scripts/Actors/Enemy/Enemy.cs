using Actors.Player;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

// Note: Object must be spawned facing to the Right!
namespace Actors.Enemy
{
    [RequireComponent(typeof(EnemyHealth))]
    public class Enemy : Walker
    {
        [FormerlySerializedAs("attackSound")] [SerializeField] protected AudioClip AttackSound;
        [FormerlySerializedAs("awarenessRadius")] [SerializeField] [Range(1, 50)] protected float AwarenessRadius = 10;
        [FormerlySerializedAs("attackRange")] [SerializeField] [Range(1, 50)] protected float AttackRange = 5;
        [FormerlySerializedAs("visionAngle")] [SerializeField] [Range(30, 360)] public float VisionAngle = 100;
        [FormerlySerializedAs("timeBetweenAttacks")] [SerializeField] [Range(0, 100)] protected float TimeBetweenAttacks = 2f; // in seconds

        [HideInInspector] public bool IsAttacking = false;
        protected float TimeSinceLastAttack = -float.NegativeInfinity;
        protected bool CanAttack = true;

        // Components
        private GrappleHook m_grapple;
        protected EnemyAi Ai;

        /// <summary>
        /// Never override Awake without invoking base.Awake()
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            Ai = GetComponent<EnemyAi>();
            m_grapple = GameComponents.Player.GetComponent<GrappleHook>();
            gameObject.layer = LayerMask.NameToLayer("Enemy");

            if (!Targeting.Target)
                Targeting.Target = GameComponents.Player.transform;
        }

        protected virtual void Start()
        {
            CanAttack = false;
            // wait a random delay until allowed to attack
            Invoke(nameof(AllowAttack), Random.Range(0.5f, 2));
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

                if (TimeBetweenAttacks < TimeSinceLastAttack && IsInAttackRange && !TargetIsDead && !IsGrappled)
                {
                    Attack();
                    TimeSinceLastAttack = 0;
                }

                MoveToTarget();
                FaceAimDirection();
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
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
                Invoke(nameof(Attack), 0.5f);
                return;
            }

            if (AttackSound)
                AudioSource.PlayOneShot(AttackSound);

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
                return Targeting.AimDirection.magnitude < AttackRange && Mathf.Abs(angle) < VisionAngle ||
                       Targeting.AimDirection.magnitude < (AttackRange * 0.4f); // the small circlular raduis
            }
        }

        public bool IsGrappled => m_grapple != null && gameObject == m_grapple.GrabbedObj;

        public bool IsAware => Targeting.Target && Targeting.AimDirection.magnitude < AwarenessRadius;

        public bool TargetIsDead => Targeting.Target && Targeting.Target.GetComponent<Health>().IsDead;

        // used in animations
        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, AwarenessRadius);


            Gizmos.color = Color.red;
            //Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.DrawLine(transform.position,
                transform.position + Quaternion.AngleAxis(VisionAngle / 2, Vector3.back) * Vector3.right * FacingSign *
                AttackRange);
            Gizmos.DrawLine(transform.position,
                transform.position + Quaternion.AngleAxis(VisionAngle / 2, Vector3.forward) * Vector3.right * FacingSign *
                AttackRange);
            Gizmos.DrawWireSphere(transform.position, (AttackRange * 0.4f)); // the small circlular raduis
        }
    }
}