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
    [HideInInspector] public bool m_HasDoubleJump = true;


    /// <summary> the minimum horizontal speed that the player should be moving in to perform a dash attack </summary>
    private float minDashAttackSpeedThr;

    //Components
    private GrappleHook grapple;
    private PlayerAttack playerAttack;
    private Text statsText;
    private MomentumManager _momentumManager;


    protected override void Awake()
    {
        base.Awake();

        grapple = GetComponent<GrappleHook>();
        playerAttack = GetComponent<PlayerAttack>();
        _momentumManager = GetComponent<MomentumManager>();

        statsText = GameObject.Find("Stats text").GetComponent<Text>();
        m_DefaultAnimSpeed = _anim.speed;
        minDashAttackSpeedThr = moveSpeed * 0.5f;
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
            health.Die();
        }

        //Get jump input
        if (!m_Jump && !_anim.GetBool("Slamming"))
        {
            m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
        }

        //If not slamming, update where player is facing
        if (!_anim.GetBool("Slamming"))
        {
            FaceAimDirection();
        }
    }

    void FixedUpdate()
    {
        Grounded = false;

        // Recharge the doubleJump once one ground
        if (Grounded) m_HasDoubleJump = true;

        Move();
        m_Jump = false;
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
        Vector2 d = targeting.AimDirection * FacingSign;
        var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public override void Flip()
    {
        base.Flip();

        // if the player is pulling an object, then flipping will move the grabbedObject to that other side as well
        if (grapple.m_Pulling && grapple.GrabbedObj != null &&
            grapple.GrabbedObj.GetComponent<Rigidbody2D>().bodyType != RigidbodyType2D.Static)
        {
            // the offset between the player and the grabbedObj
            var offsetFromGrabbedObj = (grapple.GrabbedObj.transform.position - transform.position) * FacingSign;
            // this check prevents the player from moving enemies that are way too far
            if (offsetFromGrabbedObj.magnitude < 2)
                grapple.GrabbedObj.transform.position += offsetFromGrabbedObj;
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

    // TODO: when wallclimbing, if input_Y is max (1), player will not slide (stay hanging on the wall)

    private float momentum
    {
        get { return _momentumManager.Momentum; }
    }

    /// <summary>
    /// If you want to modify any movements or Rigidbody forces/velocity do that here,
    /// otherwise your changes will be immediately overriden by this method as the velocity is modified directly. </summary>
    private void Move()
    {
        if (_anim.GetBool("DashAttack"))
            return;
        if (!BlockMoveInput)
            if (control_AirControl || Grounded)
                _move = MovementInput.x * moveSpeed * momentum;

        // If reached jump peak
        if (Mathf.Approximately(Rb.velocity.y, 0) && !Grounded)
        {
        }

        // If on the wall
        if (!Grounded && Wallcheck && _move * Mathf.Sign(FacingSign) > 0 && !_anim.GetBool("Slamming"))
        {
            // On wall and falling
            if (Rb.velocity.y < 0)
            {
                m_HasDoubleJump = true;

                // limit wallslide speed
                if (Rb.velocity.y < -wallSlideSpeedMax)
                {
                    Rb.velocity = new Vector2(Rb.velocity.x, -wallSlideSpeedMax);
                }

//                else if (MovementInput.y > 0.5f)
//                {
//                    print("trying to resist wall slide");
//                }
            }
        }

        if (Grounded)
        {
            if (_anim.GetBool("Slamming")) // Stop moving on slam landing
                _move = 0;
        }
        // TODO: on jump buttonUp, if(!m_Jump && Grounded) set rb Y velocity to zero 0

        // Move horizontally
        Rb.velocity = /*grapple.m_Flying? Vector2.zero:*/ new Vector2(_move, Rb.velocity.y);

        if (Wallcheck)
            m_HasDoubleJump = true;

        //When to jump
        if (m_Jump)
        {
            if (grapple.m_Flying)
                grapple.EndGrapple();

            if (Grounded)
            {
                m_lastGroundSpeed = _move; //Updating lastGroundSpeed
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
                m_Jump = false;
            }
            else if (control_PlayerCanDoubleJump && m_HasDoubleJump)
            {
                // Double jump
                Rb.velocity = new Vector2(0, 0); // Resets vertical speed before doubleJump (prevents glitchy jumping)
                print("Double jump");
                if (!grapple.m_Flying
                ) // if grappling, then the player get's an extra jump, otherwise mark that the doubleJump has been used
                    m_HasDoubleJump = false;
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
        String clipName = _anim.GetCurrentAnimatorClipInfo(_anim.layerCount - 1)[_anim.layerCount - 1].clip.name;

        if (_anim.GetBool("Slamming") && playerAttack.HasReachedSlamPeak)
        {
            //Is using slam_attack and reached peak
            _anim.speed = 0;
        }
        else if (clipName.Equals("Walk") && Mathf.Abs(Rb.velocity.x) >= 0.1)
        {
            //walking animation and moving
            _anim.speed = Mathf.Abs(Rb.velocity.x * animationSpeedCoeff);
        }
        else if (clipName.Equals("Air Idle") && Mathf.Abs(Rb.velocity.y) >= 0.1)
        {
            //Airborn animation and moving
            _anim.speed = Mathf.Abs(Mathf.Log(Mathf.Abs(Rb.velocity.y * animationSpeedCoeff * 5f / 8f)));
        }
        else
        {
            _anim.speed = m_DefaultAnimSpeed; //Go back to default speed
            playerAttack.HasReachedSlamPeak = false;
        }

        _anim.speed *= momentum;
    }

    public bool CanDashAttack
    {
        get { return Mathf.Abs(Rb.velocity.x) > minDashAttackSpeedThr * momentum; }
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
            m_Climb = Physics2D.OverlapCircleAll(m_ClimbCheck.position, k_WallcheckRaduis, floorMask)
                .Any(hit => hit.transform != transform && !hit.transform.IsChildOf(this.transform));
            return m_Climb;
        }
        set { m_Climb = value; }
    }

    public override void UpdateAnimatorParams()
    {
        base.UpdateAnimatorParams();
        _anim.SetBool("Grappling", grapple.m_Flying);
        _anim.SetFloat("VSpeed", Mathf.Abs(Rb.velocity.y));
        _anim.SetBool("Grounded", Grounded);

        if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("Attack Dash"))
        {
            playerAttack.DashAttackClose();
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
            _anim.SetBool("Landing", true);
            Debug.Log("Landing");
        }
        else
        {
            _anim.SetBool("Landing", false);
        }
    }

    private void Debug_UpdateStats()
    {
        statsText.text = string.Join("\n", new[]
        {
            "timeScale: " + Time.timeScale,
            "fixedDeltaTime: " + Time.fixedDeltaTime,
            "velocity: " + Rb.velocity,
            "HSpeed(Anim): " + _anim.GetFloat("Speed"),
            "VSpeed(Anim): " + _anim.GetFloat("VSpeed"),
            "lastGroundSpeed: " + m_lastGroundSpeed,
            "BlockInput: " + BlockMoveInput,
            "Doublejump available? " + m_HasDoubleJump,
            "Wallcheck: " + Wallcheck,
            "Grounded: " + Grounded,
            "Jump: " + m_Jump,
            "Grappling: " + (
                grapple.m_Flying ? "flying" :
                grapple.m_Pulling ? "pulling" :
                "none"
            )
        });
    }


    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.DrawWireSphere(m_ClimbCheck.position, k_WallcheckRaduis);
    }
}