using System.Collections;
using Pathfinding;
using UnityEngine;
using UnityEngine.Serialization;
//Note this line, if it is left out, the script won't know that the class 'Path' exists and it will throw compiler errors
//This line should always be present at the top of scripts which use pathfinding

namespace Actors.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Seeker))]
    [RequireComponent(typeof(Enemy))]
    public class EnemyAi : Targeting
    {
        [FormerlySerializedAs("updateDelay")] [SerializeField] private float m_updateDelay = 1f;


        [FormerlySerializedAs("stopDist")] [SerializeField] private float m_stopDist = 2f;
        [FormerlySerializedAs("followOnGround")] [SerializeField] public bool FollowOnGround = true;
    
        /// <summary> debug levels: 0, 1, 2, 3.    0 shoes none </summary>
        [FormerlySerializedAs("_debugLevel")] [SerializeField] private int m_debugLevel = 0;

        //The max distance from the AI to a waypoint for it to continue to the next waypoint
        [FormerlySerializedAs("nextWaypointDistance")] [SerializeField] private float m_nextWaypointDistance = 3;

        // experimental
        [FormerlySerializedAs("heightToJump")] [SerializeField] private float m_heightToJump = 1;
        [FormerlySerializedAs("angleToJump")] [SerializeField] private float m_angleToJump = 45;

        //The calculated path
        private Path m_path;
    
        //The point to move to
        private Vector3 m_targetPosition;
    
        private bool m_pathHasEnded = false;

        //The waypoint we are currently moving towards
        private int m_currentWaypoint = 0;
        private readonly float m_kGroundedRadius = .2f;

        private Vector3 m_moveDirection;

        private Seeker m_seeker;
        private Enemy m_enemy;
        private Targeting m_targeting;
        private Animator m_anim;


        protected virtual void Awake()
        {
            m_targeting = GetComponent<Targeting>();
            m_anim = GetComponent<Animator>();
            m_seeker = GetComponent<Seeker>();
            m_enemy = GetComponent<Enemy>();
            transform.Find("GroundCheck");
        }

        protected virtual void Start()
        {
            if (!TargetExists()) return;

            m_targetPosition = Target.position;
            //stopDist = enemyScript.awarenessRadius * 0.75f;
            //Start a new path to the targetPosition, return the result to the OnPathComplete function
            m_seeker.StartPath(transform.position, m_targetPosition, OnPathComplete);

            StartCoroutine(UpdatePathWithDelayRecursive());
        }


        /// <summary>
        /// if (!m_Attacking && !isDead) ApproachTarget();
        /// </summary>
        public void MoveAlongPath()
        {
            if (!m_enemy.IsAttacking && !m_enemy.Health.IsDead)
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

                if (m_debugLevel > 2)
                    Debug.Log("CalculateWaypoint result is FALSE :(");
                m_moveDirection = Vector3.zero;
                if (m_anim) m_anim.SetBool("Walk", false);
                return;
            }

            Vector3 dir = m_moveDirection * Time.fixedDeltaTime;

            Debug.DrawLine(transform.position, transform.position + dir, Color.yellow);
            Debug.DrawLine(transform.position, Target.position, Color.red);

            if (!FollowOnGround || FollowOnGround && m_enemy.Grounded)
                m_enemy.Move(dir);

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
                if (m_debugLevel >= 1)
                    Debug.LogWarning(name + " Target is null");
                return false;
            }

            m_targetPosition = Target.position;


            //We have no path to move after yet
            if (m_path == null)
            {
                if (m_debugLevel >= 1)
                    Debug.LogWarning(name + " path == null");
                UpdatePath();
                return false;
            }

            // If path ended
            if (m_currentWaypoint >= m_path.vectorPath.Count)
            {
                if (m_debugLevel >= 2)
                    Debug.Log("End Of Path Reached");
                if (m_pathHasEnded)
                    return false;
                m_enemy.Rb.velocity = new Vector2(0, m_enemy.Rb.velocity.y - m_enemy.Rb.gravityScale);

                m_pathHasEnded = true;
                return false;
            }

            m_pathHasEnded = false;

            // If too close
            if (m_stopDist > Vector3.Distance(transform.position, Target.position) && TargetExists())
            {
                return false;
            }

            if (m_anim != null) m_anim.SetBool("Walk", true);
            Vector3 nextWaypoint = m_path.vectorPath[m_currentWaypoint] - transform.position;

            //Check if we are close enough to the next waypoint. If we are, proceed to follow the next waypoint
            if (nextWaypoint.magnitude < m_nextWaypointDistance) m_currentWaypoint++;

            //Direction to the next waypoint
            Vector2 dir = nextWaypoint;
            if (FollowOnGround)
                dir = new Vector2(dir.x, m_enemy.Rb.velocity.y - m_enemy.Rb.gravityScale);
            m_moveDirection = dir;
            return true;
        }

        private IEnumerator UpdatePathWithDelayRecursive()
        {
            if (TargetExists())
            {
                UpdatePath();

                if (m_updateDelay > 0) yield return new WaitForSeconds(m_updateDelay);
                StartCoroutine(UpdatePathWithDelayRecursive());
            }
        }

        private void UpdatePath()
        {
            if (TargetExists())
            {
                m_targetPosition = Target.position;

                //Start a new path to the targetPosition, return the result to the OnPathComplete function
                m_seeker.StartPath(transform.position, m_targetPosition, OnPathComplete);
            }
        }

        public void OnPathComplete(Path p)
        {
            if (!p.error)
            {
                m_path = p;
                m_currentWaypoint = 0; //Reset the waypoint counter
            }
        }

        /// <summary>
        /// returns true if there is a Target
        /// </summary>
        /// <returns>returns true if there is a Target</returns>
        private bool TargetExists()
        {
            Transform tr = m_targeting.Target;
            if (tr != null) Target = tr.transform;

            //GameObject go = GameObject.FindGameObjectWithTag("Player");
            //if (go != null) Target = go.transform;

            if (Target == null)
            {
                if (m_debugLevel >= 1)
                    Debug.Log("Target is null");
                return false;
            }

            return true;
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = m_enemy && m_enemy.IsAware ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, m_stopDist);
        }
    }
}