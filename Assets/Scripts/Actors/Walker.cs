using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Actors
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Targeting))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Walker : MonoBehaviour
    {
        protected Animator Anim;

        [SerializeField] protected float AnimationSpeedCoeff = 0.8f;
        protected AudioSource AudioSource;
        protected bool Climbing = false;

        //TODO: put these under a Control struct
        [SerializeField] protected bool control_AirControl = true;
        [SerializeField] protected bool control_facesAimDirection = true;

        /// <summary> Storing the default animation speed, so that we can go back to it after changing it. </summary>
        protected float DefaultAnimSpeed;

        public bool FacingRight { get; protected set; } = true;
        [SerializeField] protected LayerMask floorMask;

        [SerializeField] protected AudioClip footstepSound;

        [SerializeField] [Range(0f, 5f)]
        protected float GroundedRadius = 0.08f; // Radius of the overlap circle to determine if grounded

        [SerializeField] protected float JumpForce = 8f;

        protected float LastGroundSpeed;
        [SerializeField] protected Transform m_GroundCheck;

        private bool m_grounded = true;

        /// <summary> A float indicating the _intended_ horizontal movement. The player will be trying to move at this speed. </summary>
        protected float Movement;


        [SerializeField] protected float MoveSpeed = 3f;
        [SerializeField] protected bool NeverFlip;
        protected Targeting Targeting;

        // event is triggered when is at jump peak (was going up then started going down)
        public event Action OnJumpPeak;

    [SerializeField] protected AudioClip footstepSound;
    [SerializeField] protected Transform m_GroundCheck;

        /// Is a jump pending? Should the character jump on the next update?
        protected bool ToJump { get; set; }

        //Components
        public Rigidbody2D Rb { get; private set; }
        public Health Health { get; private set; }

        public bool Grounded
        {
            get
            {
                m_grounded = false;
                m_grounded = Physics2D.OverlapCircleAll(m_GroundCheck.position, GroundedRadius, floorMask)
                    .Any(col => col.gameObject != gameObject && !col.gameObject.transform.IsChildOf(transform));

                return m_grounded;
            }
            set => m_grounded = value;
        }


        /// <summary>
        ///     if aiming
        ///     RIGHT: return (1),
        ///     LEFT: return (-1)
        /// </summary>
        public int FacingSign => FacingRight ? 1 : -1;

        /// <summary> If set to true, all movement inputs will be ignored </summary>
        public bool IsMoveInputBlocked { get; set; }

        // ==


        /// <summary>
        ///     Set component fields:
        ///     - Targeting,
        ///     - AudioSource,
        ///     - Rigidbody2D,
        ///     - Animator,
        ///     - Health
        /// </summary>
        protected virtual void Awake()
        {
            Targeting = GetComponent<Targeting>();
            if (!Targeting)
            {
                Targeting = gameObject.AddComponent<Targeting>();
                Debug.Log("Adding Targeting component to " + gameObject.name);
            }

            AudioSource = GetComponent<AudioSource>();
            Rb = GetComponent<Rigidbody2D>();
            Anim = GetComponent<Animator>();
            Health = GetComponent<Health>();

            if (!m_GroundCheck) m_GroundCheck = transform.Find("GroundCheck");
            if (!m_GroundCheck) m_GroundCheck = transform;

            DefaultAnimSpeed = Anim.speed;

            Debug.Assert(Targeting != null, "targeting!=null");
        }


        public void Move(Vector2 input)
        {
            if (!IsMoveInputBlocked)
                if (control_AirControl || Grounded)
                    Movement = input.x * MoveSpeed;

            // Move (horizontally only)
            Rb.velocity = input * MoveSpeed;
        }

        /// <summary>
        ///     This method is called when the Move() method is asked to jump.
        ///     This method checks for the conditions of being allowed to jump (such as jumping only when being grounded)
        /// </summary>
        public void CallJump()
        {
            if (Grounded)
            {
                LastGroundSpeed = Movement; //Updating lastGroundSpeed
                Jump();
            }
        }

        public void Jump()
        {
            Rb.AddForce(Vector2.up * Rb.mass * JumpForce, ForceMode2D.Impulse);
        }

        protected virtual void LateUpdate()
        {

        }

        /// Adjusts animation speed when walking, falling, or slamming
        protected virtual void AdjustAnimationSpeed()
        {
            var clipName = Anim.GetCurrentAnimatorClipInfo(Anim.layerCount - 1)[Anim.layerCount - 1].clip.name;

            if (clipName.Equals("Walk") && Mathf.Abs(Rb.velocity.x) >= 0.1)
                Anim.speed = Mathf.Abs(Rb.velocity.x * AnimationSpeedCoeff);
            else if (clipName.Equals("Air Idle") && Mathf.Abs(Rb.velocity.y) >= 0.1)
                Anim.speed = Mathf.Log(Mathf.Abs(Rb.velocity.y * AnimationSpeedCoeff * 5f / 8f));
            else
                Anim.speed = DefaultAnimSpeed; //Go back to default speed
        }


        /// <summary>
        ///     Does not allow player to move for a given time in seconds.
        ///     Hint: To block during an animation use the anim.GetCurrentAnimatorClipInfo(0).GetLength(0) to get the time of the
        ///     animation
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="delay"></param>
        public IEnumerator BlockMoveInput(float duration, float delay = 0)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            IsMoveInputBlocked = true;

            yield return new WaitForSeconds(duration);
            IsMoveInputBlocked = false;
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            // Play footstep sound, if player falls fast enough
            if (collision.relativeVelocity.y * Time.deltaTime < 2) AudioSource.PlayOneShot(footstepSound, 0.5f);
        }

        /// <summary>
        ///     Updating the animatorController parameters: [Grounded, VSpeed, Speed]
        ///     this method should be overriden and extended to add any extra animator params.
        /// </summary>
        protected virtual void UpdateAnimatorParams()
        {
            Anim.SetFloat("Speed", Mathf.Abs(Rb.velocity.x));
        }

        public virtual void Flip()
        {
            if (NeverFlip) return;
            FacingRight = !FacingRight;
            var theScale = transform.localScale;
            theScale.x = -1 * theScale.x;
            transform.localScale = theScale;

            //Flip healthbar
            var healthBarScale = Health.healthBar.transform.localScale;
            healthBarScale.x = -healthBarScale.x;
            Health.healthBar.transform.localScale = healthBarScale;
        }

        /// Uses the `FacingRight` and `m_facesAimDirection` to Flip the player's direction
        protected void FaceAimDirection()
        {
            if (!Grounded && !control_AirControl) return;
            FaceDirection(control_facesAimDirection ? Targeting.AimDirection.x : Movement);
        }

        protected void FaceDirection(float directionX)
        {
            if (FacingRight && directionX < 0) Flip(); // If directionX is left and facing right, Flip()
            else if (!FacingRight && directionX > 0) Flip(); // If directionX is right and facing left, Flip()
        }

        public void PlayFootstepSound()
        {
            AudioSource.PlayOneShot(footstepSound, 0.5f);
        }

        protected virtual void OnDrawGizmos()
        {
            if (m_GroundCheck != null)
                Gizmos.DrawWireSphere(m_GroundCheck.position, GroundedRadius);
        }
    }
}
//using static Input_AimDirection.AimDirection;