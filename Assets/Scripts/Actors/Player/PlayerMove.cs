using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

//using static Input_AimDirection.AimDirection;

namespace Actors.Player
{
    public class PlayerMove : Walker
    {
        [FormerlySerializedAs("m_CeilingCheck")] [SerializeField]
        protected Transform CeilingCheck; // A position marking where to check for ceilings

        [FormerlySerializedAs("m_ClimbCheck")] [SerializeField]
        protected Transform ClimbCheck;

        [FormerlySerializedAs("arrow")] [SerializeField]
        private GameObject m_arrow;

        [FormerlySerializedAs("control_PlayerCanDoubleJump")] [SerializeField]
        private bool m_controlPlayerCanDoubleJump = true;

        [FormerlySerializedAs("fallMultiplier")] [SerializeField]
        private float m_fallMultiplier = 2.5f;


        //Components
        private GrappleHook m_grapple;

        /// how fast to lerp to original gravity?
        [Range(0, 1)] [SerializeField] private float m_gravityScaleLerp = 0.5f;

        private bool m_hasDoubleJump = true;

        [SerializeField] [Range(0f, 5f)] private float m_kWallcheckRaduis = 0.3f;

        [FormerlySerializedAs("lowJumpMultiplier")] [SerializeField]
        private float m_lowJumpMultiplier = 2f;

        private MomentumManager m_momentumManager;

        /// the gravityScale the rb started with, we save this so that we can lerp to this original value
        private float m_originalGravityScale;

        private PlayerAttack m_playerAttack;
        private Text m_statsText;

        // lerper
        private float m_t;

        [FormerlySerializedAs("walljumpForce")] [SerializeField]
        private float m_walljumpForce = 4f;

        [SerializeField] protected float WallSlideSpeedMax = 1;


        // = properties

        public static Vector2 MovementInput => new Vector2(
            CrossPlatformInputManager.GetAxis("Horizontal"),
            CrossPlatformInputManager.GetAxis("Vertical")
        );

        private float Momentum => m_momentumManager.Momentum;

        public bool CanDashAttack => Mathf.Abs(Rb.velocity.x) > MoveSpeed * 0.5f * Momentum;

        public bool CeilCheck => Physics2D
            .OverlapCircleAll(CeilingCheck.position, GroundedRadius, floorMask)
            .Any(col => col.gameObject != gameObject);

        /// assigns Climbing and returns the new value
        public bool WallCheck => Climbing = Physics2D
            .OverlapCircleAll(ClimbCheck.position, m_kWallcheckRaduis, floorMask)
            .Any(hit => hit.transform != transform && !hit.transform.IsChildOf(transform));


        // == Unity methods

        protected override void Awake()
        {
            base.Awake();

            m_grapple = GetComponent<GrappleHook>();
            m_playerAttack = GetComponent<PlayerAttack>();
            m_momentumManager = GetComponent<MomentumManager>();

            m_statsText = GameObject.Find("Stats text").GetComponent<Text>();
            DefaultAnimSpeed = Anim.speed;
            gameObject.layer = LayerMask.NameToLayer("Player");

            if (!m_arrow) m_arrow = transform.Find("Arrow").gameObject;

            if (!CeilingCheck) CeilingCheck = transform.Find("CeilingCheck");
            if (!ClimbCheck) ClimbCheck = transform.Find("ClimbCheck");
            if (!CeilingCheck) CeilingCheck = transform;
            if (!ClimbCheck) ClimbCheck = transform;

            m_originalGravityScale = Rb.gravityScale;
        }

        private void Update()
        {
            m_t += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Delete)) Health.Die();

            //Get jump input
            if (!ToJump && !Anim.GetBool("Slamming")) ToJump = CrossPlatformInputManager.GetButtonDown("Jump");

            //If not slamming, update where player is facing
            if (!Anim.GetBool("Slamming")) FaceAimDirection();


            // lerp the gravity back to the og. The current scale will converge at the rate of m_gravityScaleLerp*Rb.gravityScale each second 
//        Rb.gravityScale = Mathf.Lerp(Rb.gravityScale, m_originalGravityScale, Time.deltaTime * m_gravityScaleLerp);
        }

        private void FixedUpdate()
        {
            Grounded = false;

            // Recharge the doubleJump once one ground
            var currentlyGrounded = Grounded;

            if (currentlyGrounded)
                m_hasDoubleJump = true;

            if (currentlyGrounded) Rb.gravityScale = m_originalGravityScale;

            Move();
            ToJump = false;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            AdjustAnimationSpeed();

            UpdateAnimatorParams();
            Debug_UpdateStats();

            RotateArrow();
        }


        // == methods

        private void RotateArrow()
        {
            var d = Targeting.AimDirection * FacingSign;
            var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            m_arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        public override void Flip()
        {
            base.Flip();

            // if the player is pulling an object, then flipping will move the grabbedObject to that other side as well
            if (m_grapple.Pulling && m_grapple.GrabbedObj != null &&
                m_grapple.GrabbedObj.GetComponent<Rigidbody2D>().bodyType != RigidbodyType2D.Static)
            {
                // the offset between the player and the grabbedObj
                var offsetFromGrabbedObj =
                    (m_grapple.GrabbedObj.transform.position - transform.position) * FacingSign;
                // this check prevents the player from moving enemies that are way too far
                if (offsetFromGrabbedObj.magnitude < 2)
                    m_grapple.GrabbedObj.transform.position += offsetFromGrabbedObj;
            }
        }

        /// <summary>
        ///     Changes the fall speed of the player depending on the jump input.
        ///     This is how long and short jump presses are different
        /// </summary>
        private void ModifyGravity()
        {
            var gravityV2 = Vector2.up * Physics2D.gravity.y * Time.fixedDeltaTime;
            if (Rb.velocity.y < 0.1f)
                Rb.velocity += gravityV2 * m_fallMultiplier;
            else if (Rb.velocity.y > 0 && !CrossPlatformInputManager.GetButton("Jump"))
                Rb.velocity += gravityV2 * m_lowJumpMultiplier;
        }

        /// <summary>
        ///     If you want to modify any movements or Rigidbody forces/velocity do that here,
        ///     otherwise your changes will be immediately overriden by this method as the velocity is modified directly.
        /// </summary>
        private void Move()
        {
            if (IsMoveInputBlocked)
            {
                print(name + ": Input is blocked, not gonna move");
                Movement = 0;
                return;
            }

            if (Anim.GetBool("DashAttack"))
                return;

            if (control_AirControl || Grounded)
                Movement = MovementInput.x * MoveSpeed * Momentum;


            // If reached jump peak
            if (Mathf.Approximately(Rb.velocity.y, 0) && !Grounded)
            {
            }

            // If on the wall
            if (!Grounded && WallCheck && Movement * Mathf.Sign(FacingSign) > 0 && !Anim.GetBool("Slamming"))
                if (Rb.velocity.y < 0)
                {
                    m_hasDoubleJump = true;

                    // limit wallslide speed
                    if (Rb.velocity.y < -WallSlideSpeedMax)
                        Rb.velocity = new Vector2(Rb.velocity.x, -WallSlideSpeedMax);
                }

            if (Grounded && Anim.GetBool("Slamming")) // Stop moving on slam landing
            {
                Movement = 0;
                Anim.SetBool("Slamming", false);
            }

            // TODO: on jump buttonUp, if(!m_Jump && Grounded) set rb Y velocity to zero 0

            // Move horizontally
            if (Mathf.Abs(Movement) > 0.05f)
                Rb.velocity = /*grapple.m_Flying? Vector2.zero:*/ new Vector2(Movement, Rb.velocity.y);

            if (WallCheck)
                m_hasDoubleJump = true;

            //When to jump
            if (ToJump)
            {
                if (m_grapple.Flying)
                    m_grapple.EndGrapple();

                if (Grounded)
                {
                    LastGroundSpeed = Movement; //Updating lastGroundSpeed
                    Jump();
                }
                else if (WallCheck)
                {
                    //If jumping and on the wall:
                    // NOTE: the player has to be facing the wall to wallslide/walljump
                    Debug.Log("Jump off wall");
                    Rb.AddForce(Vector2.right * -FacingSign * m_walljumpForce * JumpForce * Rb.mass,
                        ForceMode2D.Impulse);

                    Jump();
                    Flip();
                    ToJump = false;
                }
                else if (m_controlPlayerCanDoubleJump && m_hasDoubleJump)
                {
                    // Double jump
                    // Resets vertical speed before doubleJump (prevents glitchy jumping)
                    Rb.velocity = new Vector2(Rb.velocity.x, 0);
                    print("Double jump");

                    // if grappling, then the player gets an extra jump, otherwise mark that the doubleJump has been used
                    if (!m_grapple.Flying)
                        m_hasDoubleJump = false;

                    Jump();
                }
            }

            if (!WallCheck) ModifyGravity();
        }

        /// <inheritdoc />
        /// <summary> Adjusts animation speed when walking, falling, or slamming.</summary>
        protected override void AdjustAnimationSpeed()
        {
            var clipName = Anim.GetCurrentAnimatorClipInfo(Anim.layerCount - 1)[Anim.layerCount - 1].clip.name;

            if (Anim.GetBool("Slamming") && m_playerAttack.HasReachedSlamPeak)
            {
                //Is using slam_attack and reached peak
                Anim.speed = 0;
            }
            else if (clipName.Equals("Walk") && Mathf.Abs(Rb.velocity.x) >= 0.1)
            {
                //walking animation and moving
                Anim.speed = Mathf.Abs(Rb.velocity.x * AnimationSpeedCoeff);
            }
            else if (clipName.Equals("Air Idle") && Mathf.Abs(Rb.velocity.y) >= 0.1)
            {
                //Airborn animation and moving
                Anim.speed = Mathf.Abs(Mathf.Log(Mathf.Abs(Rb.velocity.y * AnimationSpeedCoeff * 5f / 8f)));
            }
            else
            {
                Anim.speed = DefaultAnimSpeed; //Go back to default speed
                m_playerAttack.HasReachedSlamPeak = false;
            }

            Anim.speed *= Momentum;
        }

        protected override void UpdateAnimatorParams()
        {
            base.UpdateAnimatorParams();
            Anim.SetBool("Grappling", m_grapple.Flying);
            Anim.SetFloat("VSpeed", Mathf.Abs(Rb.velocity.y));
            Anim.SetBool("Grounded", Grounded);


            if (!Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack Dash")) m_playerAttack.DashAttackClose();
        }

        private void CheckLanding()
        {
            //If should be landing
            var landingHeight = Mathf.Abs(Rb.velocity.y) * 0.5f;
            Debug.DrawLine(m_GroundCheck.position, (Vector2) m_GroundCheck.position + Vector2.down * landingHeight,
                Color.green);
            if (Physics2D.Raycast(m_GroundCheck.position, Vector2.down, landingHeight) && !Grounded &&
                Rb.velocity.y < 0)
            {
                Anim.SetBool("Landing", true);
                Debug.Log("Landing");
            }
            else
            {
                Anim.SetBool("Landing", false);
            }
        }

        private void Debug_UpdateStats()
        {
#if !UNITY_EDITOR
        m_statsText.enabled = false;
#endif
            m_statsText.text = string.Join("\n",
                "timeScale: \t" + Time.timeScale,
                "fixedDeltaTime: \t" + Time.fixedDeltaTime,
//            "input: \t" + input,
                "intended movement: \t" + Movement,
                "Rb.velocity: \t" + Rb.velocity,
                "(HSpeed, VSpeed): \t" + $"{Anim.GetFloat("Speed")}, {Anim.GetFloat("VSpeed")}",
                "lastGroundSpeed: \t" + LastGroundSpeed,
                "BlockInput: \t" + IsMoveInputBlocked,
                "Doublejump available? " + m_hasDoubleJump,
                "Wallcheck: \t" + WallCheck,
                "Grounded: \t" + Grounded,
                "Jump: \t" + ToJump,
                "Grappling: \t" + (
                    m_grapple.Flying ? "flying" :
                    m_grapple.Pulling ? "pulling" :
                    "none"
                )
            );
        }


        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.DrawWireSphere(ClimbCheck.position, m_kWallcheckRaduis);
        }
    }
}