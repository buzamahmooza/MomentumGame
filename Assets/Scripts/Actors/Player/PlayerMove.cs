using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using InControl;
using Pathfinding;

//using static Input_AimDirection.AimDirection;

public class PlayerMove : Walker
{
    [SerializeField] protected Transform m_CeilingCheck; // A position marking where to check for ceilings
    [SerializeField] protected Transform m_ClimbCheck;

    [SerializeField] GameObject arrow;
    [SerializeField] private bool control_PlayerCanDoubleJump = true;

    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float walljumpForce = 4f;
    [SerializeField] [Range(0f, 5f)] private float k_WallcheckRaduis = 0.3f;
    [SerializeField] protected float wallSlideSpeedMax = 1;
    [HideInInspector] public bool HasDoubleJump = true;



    //Components
    private GrappleHook m_grapple;
    private PlayerAttack m_playerAttack;
    private Text m_statsText;
    private MomentumManager m_momentumManager;


    protected override void Awake()
    {
        base.Awake();

        m_grapple = GetComponent<GrappleHook>();
        m_playerAttack = GetComponent<PlayerAttack>();
        m_momentumManager = GetComponent<MomentumManager>();

        m_statsText = GameObject.Find("Stats text").GetComponent<Text>();
        DefaultAnimSpeed = Anim.speed;
        gameObject.layer = LayerMask.NameToLayer("Player");

        if (!arrow) arrow = transform.Find("Arrow").gameObject;

        if (!m_CeilingCheck) m_CeilingCheck = transform.Find("CeilingCheck");
        if (!m_ClimbCheck) m_ClimbCheck = transform.Find("ClimbCheck");
        if (!m_CeilingCheck) m_CeilingCheck = transform;
        if (!m_ClimbCheck) m_ClimbCheck = transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            Health.Die();
        }

        //Get jump input
        if (!ToJump && !Anim.GetBool("Slamming"))
        {
            ToJump = CrossPlatformInputManager.GetButtonDown("Jump");
        }

        //If not slamming, update where player is facing
        if (!Anim.GetBool("Slamming"))
        {
            FaceAimDirection();
        }
    }

    void FixedUpdate()
    {
        Grounded = false;

        // Recharge the doubleJump once one ground
        if (Grounded)
            HasDoubleJump = true;

        Move();
        ToJump = false;
    }

    void LateUpdate()
    {
        AdjustAnimationSpeed();

        Debug_UpdateStats();

        UpdateAnimatorParams();

        RotateArrow();
    }

    private void RotateArrow()
    {
        Vector2 d = Targeting.AimDirection * FacingSign;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public override void Flip()
    {
        base.Flip();

        // if the player is pulling an object, then flipping will move the grabbedObject to that other side as well
        if (m_grapple.Pulling && m_grapple.GrabbedObj != null &&
            m_grapple.GrabbedObj.GetComponent<Rigidbody2D>().bodyType != RigidbodyType2D.Static)
        {
            // the offset between the player and the grabbedObj
            Vector3 offsetFromGrabbedObj = (m_grapple.GrabbedObj.transform.position - transform.position) * FacingSign;
            // this check prevents the player from moving enemies that are way too far
            if (offsetFromGrabbedObj.magnitude < 2)
                m_grapple.GrabbedObj.transform.position += offsetFromGrabbedObj;
        }
    }

    public Vector2 MovementInput
    {
        get
        {
            return new Vector2(
                CrossPlatformInputManager.GetAxis("Horizontal"),
                CrossPlatformInputManager.GetAxis("Vertical")
            );
        }
    }

    /// <summary>
    /// Changes the fall speed of the player depending on the jump input.
    /// This is how long and short jump presses are different 
    /// </summary>
    private void ModifyGravity()
    {
        Vector2 gravityV2 = Vector2.up * Physics2D.gravity.y * Time.fixedDeltaTime;
        if (Rb.velocity.y < 0.1f)
        {
            // if player is falling
            Rb.velocity += gravityV2 * (fallMultiplier);
        }
        else if (Rb.velocity.y > 0 && !CrossPlatformInputManager.GetButton("Jump"))
        {
            // else if player is still moving up
            Rb.velocity += gravityV2 * (lowJumpMultiplier);
        }
    }

    private float momentum
    {
        get { return m_momentumManager.Momentum; }
    }

    /// <summary>
    /// If you want to modify any movements or Rigidbody forces/velocity do that here,
    /// otherwise your changes will be immediately overriden by this method as the velocity is modified directly. </summary>
    private void Move()
    {
        if (Anim.GetBool("DashAttack"))
            return;
        if (!BlockMoveInput)
            if (control_AirControl || Grounded)
                Movement = MovementInput.x * moveSpeed * momentum;


        // If reached jump peak
        if (Mathf.Approximately(Rb.velocity.y, 0) && !Grounded)
        {
        }

        // If on the wall
        if (!Grounded && Wallcheck && Movement * Mathf.Sign(FacingSign) > 0 && !Anim.GetBool("Slamming"))
        {
            // On wall and falling
            if (Rb.velocity.y < 0)
            {
                HasDoubleJump = true;

                // limit wallslide speed
                if (Rb.velocity.y < -wallSlideSpeedMax)
                {
                    Rb.velocity = new Vector2(Rb.velocity.x, -wallSlideSpeedMax);
                }
            }
        }

        if (Grounded)
        {
            if (Anim.GetBool("Slamming")) // Stop moving on slam landing
                Movement = 0;
        }

        // TODO: on jump buttonUp, if(!m_Jump && Grounded) set rb Y velocity to zero 0

        // Move horizontally
        Rb.velocity = /*grapple.m_Flying? Vector2.zero:*/ new Vector2(Movement, Rb.velocity.y);

        if (Wallcheck)
            HasDoubleJump = true;

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
            else if (Wallcheck)
            {
                //If jumping and on the wall:
                // NOTE: the player has to be facing the wall to wallslide/walljump
                Debug.Log("Jump off wall");
                Rb.AddForce(Vector2.right * -FacingSign * walljumpForce * jumpForce * Rb.mass, ForceMode2D.Impulse);
                Jump();
                Flip();
                ToJump = false;
            }
            else if (control_PlayerCanDoubleJump && HasDoubleJump)
            {
                // Double jump
                Rb.velocity = new Vector2(0, 0); // Resets vertical speed before doubleJump (prevents glitchy jumping)
                print("Double jump");
                if (!m_grapple.Flying
                ) // if grappling, then the player get's an extra jump, otherwise mark that the doubleJump has been used
                    HasDoubleJump = false;
                Jump();
            }

            Wallcheck = false;
        }

        if (!Wallcheck)
        {
            ModifyGravity();
        }
    }

    /// <inheritdoc />
    /// <summary> Adjusts animation speed when walking, falling, or slamming.</summary>
    protected override void AdjustAnimationSpeed()
    {
        string clipName = Anim.GetCurrentAnimatorClipInfo(Anim.layerCount - 1)[Anim.layerCount - 1].clip.name;

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

        Anim.speed *= momentum;
    }

    public bool CanDashAttack
    {
        get { return Mathf.Abs(Rb.velocity.x) > (moveSpeed * 0.5f) * momentum; }
    }

    public bool CeilCheck
    {
        get
        {
            return Physics2D
                .OverlapCircleAll(point: m_CeilingCheck.position, radius: k_GroundedRadius, layerMask: floorMask)
                .Any(col => col.gameObject != gameObject);
        }
    }

    public bool Wallcheck
    {
        get
        {
            Climbing = Physics2D.OverlapCircleAll(m_ClimbCheck.position, k_WallcheckRaduis, floorMask)
                .Any(hit => hit.transform != transform && !hit.transform.IsChildOf(this.transform));
            return Climbing;
        }
        set { Climbing = value; }
    }

    protected override void UpdateAnimatorParams()
    {
        base.UpdateAnimatorParams();
        Anim.SetBool("Grappling", m_grapple.Flying);
        Anim.SetFloat("VSpeed", Mathf.Abs(Rb.velocity.y));
        Anim.SetBool("Grounded", Grounded);

        if (!Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack Dash"))
        {
            m_playerAttack.DashAttackClose();
        }
    }

    private void CheckLanding()
    {
        //If should be landing
        float landingHeight = Mathf.Abs(Rb.velocity.y) * 0.5f;
        Debug.DrawLine(m_GroundCheck.position, (Vector2) m_GroundCheck.position + Vector2.down * landingHeight,
            Color.green);
        if (Physics2D.Raycast(m_GroundCheck.position, Vector2.down, landingHeight) && !Grounded && Rb.velocity.y < 0)
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
        print("Rb.velocity: " + Rb.velocity);
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
            "BlockInput: \t" + BlockMoveInput,
            "Doublejump available? " + HasDoubleJump,
            "Wallcheck: \t" + Wallcheck,
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
        Gizmos.DrawWireSphere(m_ClimbCheck.position, k_WallcheckRaduis);
    }
}