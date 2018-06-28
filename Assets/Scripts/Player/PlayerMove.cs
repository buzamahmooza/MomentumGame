
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using InControl;
//using static Input_AimDirection.AimDirection;

public class PlayerMove : MonoBehaviour
{

    [SerializeField] private LayerMask floorMask;
    [SerializeField] private bool control_AirJump = false;
    [SerializeField] private bool control_AirControl = true;
    [SerializeField] private bool control_PlayerCanDoubleJump = true;
    [SerializeField] private bool control_facesAimDirection;

    [SerializeField] private Vector2 speedLimit;
    [SerializeField] private float moveSpeed = 20;
    [SerializeField] private float jumpForce = 300;
    /// <summary> resolved, do not touch this </summary>
    private readonly float animationSpeedCoeff = 0.8f;
    [SerializeField] private float wallSlideSpeedMax = 1;

    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float climbSlideFactor = 0.5f;
    public float k_GroundedRadius = 0.2f; // Radius of the overlap circle to determine if grounded

    //Fields
    [HideInInspector] public bool m_Grounded = true;
    [HideInInspector] public bool m_Jump = false;
    [HideInInspector] public bool m_FacingRight = true;
    [HideInInspector] public bool m_HasDoubleJump = true;
    /// <summary> If set to true, all movement inputs will be ignored </summary>
    [HideInInspector] public bool blockMoveInput = false;

    /// <summary> Storing the default animation speed, so that we can go back to it after changing it. </summary>
    private float m_DefaultAnimSpeed;
    private float move = 0;
    private float m_lastGroundSpeed = 0;
    private float minDashAttackSpeedThr; // the minimum horizontal speed that the player should be moving in to perform a dash attack
    private bool m_Climb = false;
    private bool m_ReachedJumpPeak = false;

    //Components
    public AudioClip footstepSound;
    [HideInInspector]
    public Rigidbody2D rb;
    private Transform m_CeilingCheck;   // A position marking where to check for ceilings
    private Transform m_ClimbCheck;
    private Transform m_GroundCheck;
    private Animator _anim;
    private GrappleHookDJ grapple;
    private AudioSource audioSource;
    private PlayerAttack playerAttack;
    private HealthScript healthScript;
    private AimInput aimInput;
    private UnityEngine.UI.Text statsText;


    private void Awake() {
        m_CeilingCheck = transform.Find("CeilingCheck");
        m_ClimbCheck = transform.Find("ClimbCheck");
        m_GroundCheck = transform.Find("GroundCheck");

        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        grapple = GetComponent<GrappleHookDJ>();
        playerAttack = GetComponent<PlayerAttack>();
        aimInput = GetComponent<AimInput>();
        healthScript = GetComponent<HealthScript>();

        statsText = GameObject.Find("Stats text").GetComponent<UnityEngine.UI.Text>();
    }
    private void Start() {
        m_DefaultAnimSpeed = _anim.speed;
        minDashAttackSpeedThr = moveSpeed * 0.5f;
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    private void Update() {
        //Get jump input
        if (!m_Jump && !_anim.GetBool("Slamming")) {
            m_Jump = InputManager.ActiveDevice.Action1 || CrossPlatformInputManager.GetButtonDown("Jump");
        }

        //If not slamming, update where player is facing
        if (!_anim.GetBool("Slamming")) {
            FaceAimDirection();
        }
    }

    private void FixedUpdate() {
        Grounded = false;

        // Recharge the doubleJump once one ground
        if (Grounded) m_HasDoubleJump = true;

        Move();
        m_Jump = false;
    }
    private void LateUpdate() {
        AdjustAnimationSpeed();

        Safety_LimitSpeeds();
        Debug_UpdateStats();

        UpdateAnimatorParams();
    }

    private void ModifyGravity() {
        Vector2 gravityV2 = Vector2.up * Physics2D.gravity.y * Time.fixedDeltaTime;
        if (rb.velocity.y < 0.1f) { // if player is falling
            rb.velocity += gravityV2 * (fallMultiplier);
        } else if (rb.velocity.y > 0 && !CrossPlatformInputManager.GetButton("Jump")) { // else if player is still moving up
            rb.velocity += gravityV2 * (lowJumpMultiplier);
        }
    }
    // TODO: when wallclimbing, if input_Y is max (1), player will not slide (stay hanging on the wall)

    /// <summary>
    /// If you want to modify any movements or Rigidbody forces/velocity do that here, otherwise your changes will be immediately overriden by this method as the velocity is modified directly
    /// </summary>
    private void Move() {
        if (!blockMoveInput)
            if (control_AirControl || Grounded)
                move = CrossPlatformInputManager.GetAxis("Horizontal") * moveSpeed;

        // If reached jump peak
        if (Mathf.Approximately(rb.velocity.y, 0) && !Grounded)
            m_ReachedJumpPeak = true;

        // dynamic jump height depending on how long player presses "jump"
        /*if (CrossPlatformInputManager.GetButtonUp("Jump")) {
            print("GetButtonUp(jump)");
            if (!Grounded) {
                print("Take off");
                if (rb.velocity.y > 0) {
                    rb.velocity = new Vector2(rb.velocity.x, 0);
                    print("end jump early");
                }
            }
        }
        */

        // If on the wall
        if (!Grounded && Wallcheck && move * Mathf.Sign(FacingRightSign) > 0) {
            // On wall and falling
            if (rb.velocity.y < 0) {
                m_HasDoubleJump = true;

                // UNDONE: do something about the y velocity here

                // limit wallslide speed
                if (rb.velocity.y < -wallSlideSpeedMax) {
                    rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeedMax);
                } else if (CrossPlatformInputManager.GetAxis("Vertical") > 0.5f) {
                    print("trying to resist wall slide");
                    rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, 0, rb.velocity.y));
                }
            }
        }

        if (Grounded) {
            m_ReachedJumpPeak = false;
            if (_anim.GetBool("Slamming")) // Stop moving on slam landing
                move = 0;
        }
        // TODO: on jump buttonUp, if(!m_Jump && Grounded) set rb Y velocity to zero 0

        // Move horizontally
        rb.velocity = /*grapple.m_Flying? Vector2.zero:*/ new Vector2(move, rb.velocity.y);

        //When to jump
        if (m_Jump) {
            Wallcheck = false;
            if (grapple.m_Flying)
                grapple.EndGrapple();

            if (Grounded) {
                Debug.Log("Jump from ground");
                m_lastGroundSpeed = move; //Updating lastGroundSpeed
                Jump();
            } else if (Wallcheck) { //If jumping and on the wall:
                // NOTE: the player has to be facing the wall to wallslide/walljump
                Debug.Log("Jump off wall");
                Vector2 jumpOffWallVector = new Vector2(-FacingRightSign, 1).normalized;
                Debug.DrawLine(transform.position, transform.position + new Vector3(jumpOffWallVector.x, jumpOffWallVector.y));
                rb.velocity = jumpOffWallVector * rb.mass * jumpForce * Time.deltaTime;
                Flip();
                m_Jump = false;
            } else if (control_PlayerCanDoubleJump && m_HasDoubleJump) { // Double jump
                Debug.Log("Double jump");
                rb.velocity = new Vector2(0, 0); // Resets vertical speed before doubleJump (prevents glitchy jumping)

                if (!grapple.m_Flying) // if grappling, then the player get's an extra jump, otherwise mark that the doubleJump has been used
                    m_HasDoubleJump = false;
                Jump();
            }
        }
        if (!Wallcheck) {
            ModifyGravity();
        }
    }

    private void Jump() {
        rb.AddForce(Vector2.up * rb.mass * jumpForce * Time.fixedDeltaTime, ForceMode2D.Impulse);
    }

    /// <summary> Adjusts animation speed when walking, falling, or slamming.</summary>
    private void AdjustAnimationSpeed() {
        String clipName = _anim.GetCurrentAnimatorClipInfo(_anim.layerCount - 1)[_anim.layerCount - 1].clip.name;

        if (_anim.GetBool("Slamming") && playerAttack.HasReachedSlamPeak) { //Is using slam_attack and reached peak
            _anim.speed = 0;
        } else if (clipName.Equals("Walk") && Mathf.Abs(rb.velocity.x) >= 0.1) { //walking animation and moving
            _anim.speed = Mathf.Abs(rb.velocity.x * animationSpeedCoeff);
        } else if (clipName.Equals("Air Idle") && Mathf.Abs(rb.velocity.y) >= 0.1) {            //Airborn animation and moving
            _anim.speed = Mathf.Log(Mathf.Abs(rb.velocity.y * animationSpeedCoeff * 5f / 8f));
        } else {
            _anim.speed = m_DefaultAnimSpeed; //Go back to default speed
            playerAttack.HasReachedSlamPeak = false;
        }
    }


    public bool CanDashAttack { get { return Mathf.Abs(rb.velocity.x) > minDashAttackSpeedThr; } }
    public bool Grounded {
        get {
            // If player is not moving vertically
            /*if (Mathf.Abs(rb.velocity.y) < 0.1) {
                m_Grounded = true;
                return m_Grounded;
            }*/

            m_Grounded = false;
            m_Grounded = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, floorMask)
                .Any(col => col.gameObject != gameObject);

            return m_Grounded;
        }
        set { m_Grounded = value; }
    }
    public bool CeilCheck {
        get { return Physics2D.OverlapCircleAll(point: m_CeilingCheck.position, radius: k_GroundedRadius, layerMask: floorMask).Any(col => col.gameObject != gameObject); }
    }
    public bool Wallcheck {
        get {
            if (_anim.GetBool("Slamming")) return false;

            m_Climb = false;
            RaycastHit2D hit = Physics2D.Raycast(m_ClimbCheck.position, transform.right, k_GroundedRadius, floorMask);
            if (hit)
                m_Climb = true;
            return m_Climb;
        }
        set { m_Climb = value; }
    }
    /// <summary>
    /// if player aiming:
    ///     RIGHT return (1)
    ///     LEFT return (-1) 
    /// </summary>
    public int FacingRightSign { get { return m_FacingRight ? 1 : -1; } }

    private void OnCollisionEnter2D(Collision2D collision) {
        // Play footstep sound, if player falls fast enough
        if (rb.velocity.magnitude * Time.deltaTime <= 0) {
            audioSource.PlayOneShot(footstepSound, 0.5f);
        }
    }

    private void UpdateAnimatorParams() {
        _anim.SetBool("Grounded", Grounded);
        _anim.SetFloat("VSpeed", Mathf.Abs(rb.velocity.y));
        _anim.SetFloat("Speed", Mathf.Abs(move));
        _anim.SetBool("Grappling", grapple.m_Flying);

        if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("Attack Dash")) {
            playerAttack.DashAttackClose();
        }
    }
    public void Flip() {
        m_FacingRight = !m_FacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x = -1 * (theScale.x);
        transform.localScale = theScale;

        //Flip healthbar
        Vector3 healthBarScale = healthScript.healthBar.transform.localScale;
        healthBarScale.x = -healthBarScale.x;
        healthScript.healthBar.transform.localScale = healthBarScale;
    }
    /// <summary> Uses the <code>m_FacingRight</code> and m_facesAimDirection to Flip the player's direction </summary>
    private void FaceAimDirection() {
        // If Player should face the direction of movement,
        if (Grounded || control_AirControl) {
            // If input is opposite to where player is facing, Flip()
            if (!control_facesAimDirection) {
                if (move > 0 && !m_FacingRight) Flip();
                else if (move < 0 && m_FacingRight) Flip();
            } else { // If Player should face the AimDirection
                float aimDirX = aimInput.AimDirection.x;

                if (m_FacingRight && aimDirX < 0) Flip();// If aimInput is left and facing right, Flip()
                else if (!m_FacingRight && aimDirX > 0) Flip();// If aimInput is right and facing left, Flip()
            }
        }
        // Face the direction when grappling
        //if (grapple.m_Flying) {
        //    float xDisp = grapple.target.x - transform.position.x;

        //    if (m_FacingRight && xDisp < 0) Flip();
        //    else if (!m_FacingRight && xDisp > 0) Flip();
        //}
    }

    private void CheckLanding() {
        //If should be landing
        float landingHeight = Mathf.Abs(rb.velocity.y) * 0.5f;
        Debug.DrawLine(m_GroundCheck.position, (Vector2)m_GroundCheck.position + Vector2.down * landingHeight, Color.green);
        if (Physics2D.Raycast(m_GroundCheck.position, Vector2.down, landingHeight) && !Grounded && rb.velocity.y < 0) {
            _anim.SetBool("Landing", true);
            Debug.Log("Landing");
        } else {
            _anim.SetBool("Landing", false);
        }
    }

    public void PlayFootstepSound() {
        //audioSource.PlayOneShot(footstepSound, 0.5f);
    }

    private void Debug_UpdateStats() {
        statsText.text =
              "\nHSpeed: " + rb.velocity.x
            + "\nVSpeed: " + rb.velocity.y
            + "\nHSpeed(Anim): " + _anim.GetFloat("Speed")
            + "\nVSpeed(Anim): " + _anim.GetFloat("VSpeed")
            + "\nlastGroundSpeed: " + m_lastGroundSpeed
            + "\nBlockInput: " + blockMoveInput
            + "\nDoublejump available? " + m_HasDoubleJump
            + "\nis Wallcheck " + Wallcheck
            + "Grounded: " + Grounded
            + "\nJump: " + m_Jump
            + "\nGrappling: " + grapple.m_Flying;
    }

    /// <summary> Ensures the player doesn't fly or goes too fast </summary>
    private void Safety_LimitSpeeds() {
        //Limits hSpeed
        if (Mathf.Abs(rb.velocity.x) > speedLimit.x) {
            Debug.LogWarning("Limit HSpeed");
            float hSpeed = Mathf.Clamp(rb.velocity.x, -rb.velocity.x, rb.velocity.x);
            hSpeed = Mathf.Abs(hSpeed) * Mathf.Sign(rb.velocity.x);
            rb.velocity = new Vector2(hSpeed, rb.velocity.y);
        }
        //Limits vSpeed
        if (Mathf.Abs(rb.velocity.y) > speedLimit.y) {
            Debug.LogWarning("Limit VSpeed");
            float vSpeed = Mathf.Clamp(rb.velocity.y, -rb.velocity.y, rb.velocity.y);
            vSpeed = Mathf.Sign(rb.velocity.y) * Mathf.Abs(vSpeed);
            rb.velocity = new Vector2(rb.velocity.x, vSpeed);
        }
    }

}
