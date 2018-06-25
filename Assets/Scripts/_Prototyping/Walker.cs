
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using InControl;
//using static Input_AimDirection.AimDirection;

public class Walker : MonoBehaviour
{

#pragma warning disable CS0414 // Member hides inherited member; missing new keyword
    //Parameters
    [SerializeField] private LayerMask floorMask;
    //[SerializeField] private AnimationClip LandAnimation;
    [SerializeField] private bool m_AirJump = false;
    [SerializeField] private bool m_AirControl = true;
    [SerializeField] private bool control_PlayerCanDoubleJump = true;
    public float HspeedLimit = 20;
    public float VspeedLimit = 20;
    public float moveSpeed = 20;
    public float jumpForce = 200;
    public float animationSpeedFactor = 1.0f;
    public float wallSlideSpeedMax = 1;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;


    //Fields
    [HideInInspector]
    public bool m_Grounded = true, m_Jump = false;
    private float animSpeed, move = 0, m_lastGroundSpeed = 0;
    private Transform groundCheck;
    private RaycastHit2D hit;
    [HideInInspector] public bool m_FacingRight = true, m_HasDoubleJump = true, blockInput = false;

    //Components
    public float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
    private Transform m_CeilingCheck;   // A position marking where to check for ceilings
    private Transform m_ClimbCheck;
    private new Collider2D collider2D;
    private CircleCollider2D circle;
    [HideInInspector] public Rigidbody2D rb;
    private Animator m_Anim;
    private GrappleScript grapple;
    private AudioSource audioSource;
    public AudioClip footstepSound;
    private PlayerAttack attackScript;
    private UnityEngine.UI.Text statsText;
    public bool m_facesAimDirection;
    public float minDashAttackSpeed;
    public float climbSlideFactor = 0.5f;
    private bool m_Climb = false;
    private PlayerAttack playerAttack;


    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        collider2D = GetComponent<Collider2D>();
        playerAttack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody2D>();
        m_Anim = GetComponent<Animator>();
        circle = GetComponent<CircleCollider2D>();
        grapple = GetComponent<GrappleScript>();
        attackScript = GetComponent<PlayerAttack>();

        m_CeilingCheck = transform.Find("CeilingCheck");
        m_ClimbCheck = transform.Find("ClimbCheck");
        groundCheck = transform.Find("GroundCheck");

        statsText = GameObject.Find("StatsText").GetComponent<UnityEngine.UI.Text>();
    }

    private void Start() {
        animSpeed = m_Anim.speed;
        minDashAttackSpeed = moveSpeed * 0.8f;
    }

    private void Update() {
        //Get jump input
        if (!m_Jump && !m_Anim.GetBool("Slamming"))
            m_Jump = Device.Action1 || CrossPlatformInputManager.GetButtonDown("Jump");

        //If not slamming, update where player is facing
        if (!m_Anim.GetBool("Slamming"))
            FaceAimDirection();
    }

    private void FixedUpdate() {
        Grounded = false;

        // Recharge the doubleJump once one ground
        if (Grounded) m_HasDoubleJump = true;
        Move();
        ModifyGravity();

        AdjustAnimationSpeed();

        Safety_LimitSpeeds();
        Debug_UpdateStats();

        UpdateAnimatorParams();
        m_Jump = false;
    }

    private void ModifyGravity() {
        if (rb.velocity.y < 0)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        else if (rb.velocity.y > 0 && !m_Jump)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
    }

    // If you want to modify any movements or Rigidbody forces/velocity do that here, otherwise your changes will be immediately overriden by this method as the velocity is modified directly
    private void Move() {
        if (!blockInput)
            if (m_AirControl || Grounded)
                move = CrossPlatformInputManager.GetAxis("Horizontal") * moveSpeed;

        // If on the wall
        if (!Grounded && Wallcheck && rb.velocity.y < 0 && move * Mathf.Sign(FacingRightSign) > 0) {
            // do something about the y velocity here
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y);
            m_HasDoubleJump = true;
            if (rb.velocity.y < -wallSlideSpeedMax)
                rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeedMax);
        }

        // Stop moving on slam landing
        if (m_Anim.GetBool("Slamming"))
            if (Grounded)
                move = 0;

        rb.velocity = new Vector2(move, rb.velocity.y);

        //When to jump
        if (m_Jump) {
            Wallcheck = false;
            grapple.EndGrapple();

            if (Grounded) {
                //Updating lastGroundSpeed
                m_lastGroundSpeed = move;
                rb.AddForce(Vector2.up * rb.gravityScale * jumpForce * Time.deltaTime, ForceMode2D.Impulse);
            }
            // Double jump
            else if (control_PlayerCanDoubleJump && m_HasDoubleJump) {
                // Resets vertical speed before a doubleJump to prevent glitchy jumping
                rb.velocity = new Vector2(0, 0);
                //rb2d.velocity = new Vector2(rb2d.velocity.x, Mathf.Lerp(rb2d.velocity.y,-climbFallSpeed,Time.deltaTime));
                m_HasDoubleJump = false;
                rb.AddForce(Vector2.up * rb.gravityScale * jumpForce * Time.deltaTime, ForceMode2D.Impulse);
            }
        }
    }

    // Does NOT affect Air_Idle animation
    private void AdjustAnimationSpeed() {
        String clipName = m_Anim.GetCurrentAnimatorClipInfo(m_Anim.layerCount - 1)[m_Anim.layerCount - 1].clip.name.ToString();

        //Is using slam_attack and reached peak
        if (m_Anim.GetBool("Slamming") && playerAttack.HasReachedSlamPeak) {
            m_Anim.speed = 0;
        }
        //walking animation and moving
        else if (clipName.Equals("punchrobot_walk_flat") && Mathf.Abs(rb.velocity.x) >= 0.1) {
            m_Anim.speed = Mathf.Abs(rb.velocity.x * animationSpeedFactor);
        }
        //Airborn animation and moving
        else if (clipName.Equals("Air_Idle") && Mathf.Abs(rb.velocity.y) >= 0.1) {
            m_Anim.speed = Mathf.Abs(rb.velocity.y * animationSpeedFactor / 10.0f);
        }
        //Go back to default speed
        else {
            m_Anim.speed = animSpeed;
            playerAttack.HasReachedSlamPeak = false;
        }
    }

    /// <summary> Returns true if the player is moving fast enough to dash-attack </summary>
    public bool CanDashAttack { get { return Mathf.Abs(rb.velocity.x) >= minDashAttackSpeed; } }

    private void UpdateAnimatorParams() {
        m_Anim.SetBool("Grounded", Grounded);
        m_Anim.SetFloat("VSpeed", Mathf.Abs(rb.velocity.y));
        m_Anim.SetFloat("Speed", Mathf.Abs(move));
        m_Anim.SetBool("Grappling", grapple.m_Flying);
    }

    public void Flip() {
        m_FacingRight = !m_FacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x = -1 * (theScale.x);
        transform.localScale = theScale;
    }

    public bool Grounded {
        get {
            // If player is not moving vertically
            //if (Mathf.Abs(rb.velocity.y) < 0.1) {
            //	m_Grounded = true;
            //	return m_Grounded;
            //}
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, k_GroundedRadius, floorMask);

            m_Grounded = false;
            foreach (Collider2D col in colliders) {
                //Debug.Log("GroundCheck collided other object. Name: " + col.gameObject.name+", Layer: "+ LayerMask.LayerToName(col.gameObject.layer));
                if (col.gameObject == gameObject) break;
                else m_Grounded = true;
            }
            return m_Grounded;
        }
        set { m_Grounded = value; }
    }

    public bool Wallcheck {
        get {
            if (m_Anim.GetBool("Slamming"))
                return false;

            m_Climb = false;
            RaycastHit2D hit = Physics2D.Raycast(m_ClimbCheck.position, transform.right, k_GroundedRadius, floorMask);
            if (hit) {
                m_Climb = true;
            }
            return m_Climb;
        }
        set { m_Climb = value; }
    }


    /// <summary> Ensures the player doesn't fly or goes too fast. </summary>
    private void Safety_LimitSpeeds() {
        //Limits hSpeed
        if (Mathf.Abs(rb.velocity.x) > HspeedLimit) {
            Debug.LogWarning("Limit HSpeed");
            float hSpeed = Mathf.Clamp(rb.velocity.x, -rb.velocity.x, rb.velocity.x);
            hSpeed = Mathf.Abs(hSpeed) * Mathf.Sign(rb.velocity.x);
            rb.velocity = new Vector2(hSpeed, rb.velocity.y);
        }
        //Limits vSpeed
        if (Mathf.Abs(rb.velocity.y) > VspeedLimit) {
            Debug.LogWarning("Limit VSpeed");
            float vSpeed = Mathf.Clamp(rb.velocity.y, -rb.velocity.y, rb.velocity.y);
            vSpeed = Mathf.Sign(rb.velocity.y) * Mathf.Abs(vSpeed);
            rb.velocity = new Vector2(rb.velocity.x, vSpeed);
        }
    }

    /// <summary> Returns (1) if player is aiming right and (-1) if player is aiming left. </summary>
    // Helpful when multiplying player aim direction vectors.
    public int FacingRightSign { get { return (m_FacingRight) ? 1 : -1; } }

    private void OnCollisionEnter2D(Collision2D collision) {
        // Play footstepsound if player falls fast enough,
        if (rb.velocity.magnitude * Time.deltaTime <= 0) {
            audioSource.PlayOneShot(footstepSound, 0.5f);
        }
    }


    /// <summary> Uses the <code>m_FacingRight</code> and m_facesAimDirection to Flip the player's direction </summary>
    private void FaceAimDirection() {
        if (Grounded || m_AirControl) {
            // If we want the player to face the direction of movement
            if (!m_facesAimDirection) {
                // If the input is directed opposite to where the player is facing, Flip()
                if (move > 0 && !m_FacingRight) Flip();
                else if (move < 0 && m_FacingRight) Flip();
            } else { // If we want the player to face the AimDirection
                float aimDirX = GetComponent<AimInput>().AimDirection.x;
                // If the input is directed at where the player is facing, Flip()
                if (m_FacingRight && aimDirX < 0) Flip();
                else if (!m_FacingRight && aimDirX > 0) Flip();
            }
        }
        if (grapple.m_Flying) {
            float xDisp = grapple.target.x - transform.position.x;
            if (m_FacingRight && xDisp < 0) Flip();
            else if (!m_FacingRight && xDisp > 0) Flip();
        }
    }



    //private void CheckLanding()
    //{
    //    //If should be landing
    //    float landingHeight = Mathf.Abs(rb2d.velocity.y) * LandAnimation.length;
    //    if (hit.distance <= landingHeight && !isGrounded && !anim.GetBool("Landing") && rb2d.velocity.y < 0)
    //    {
    //        anim.SetBool("Landing", true);
    //        Debug.Log("Landing");
    //        rayStats = "Landing height";
    //    }
    //}

    public void PlayFootstepSound() {
        //audioSource.PlayOneShot (footstepSound,0.5f);
    }

    private void Debug_UpdateStats() {
        statsText.text = "Grounded: " + Grounded
                     + "\nJump: " + m_Jump
                     + "\nHSpeed: " + rb.velocity.x
                     + "\nVSpeed: " + rb.velocity.y
                     + "\nHSpeed(Anim): " + m_Anim.GetFloat("Speed")
                     + "\nVSpeed(Anim): " + m_Anim.GetFloat("VSpeed")
                     + "\nlastGroundSpeed: " + m_lastGroundSpeed
                     + "\nBlockInput: " + blockInput
                     + "\nDoublejump available? " + m_HasDoubleJump
                     + "\nis Wallcheck " + Wallcheck
                     + "\nGrappling: " + grapple.m_Flying;
    }

    private InputDevice Device { get { return InputManager.ActiveDevice; } }
}
