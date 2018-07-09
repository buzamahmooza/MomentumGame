
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using InControl;
using Pathfinding;

//using static Input_AimDirection.AimDirection;

public class PlayerMove : Walker
{
    [SerializeField] private bool control_PlayerCanDoubleJump = true;

    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    protected float momentum = 1;

    /// <summary> Storing the default animation speed, so that we can go back to it after changing it. </summary>
    private float minDashAttackSpeedThr; // the minimum horizontal speed that the player should be moving in to perform a dash attack

    //Components
    private GrappleHookDJ grapple;
    private PlayerAttack playerAttack;
    private UnityEngine.UI.Text statsText;


    protected override void Awake() {
        base.Awake();
        grapple = GetComponent<GrappleHookDJ>();
        playerAttack = GetComponent<PlayerAttack>();

        statsText = GameObject.Find("Stats text").GetComponent<UnityEngine.UI.Text>();
        m_DefaultAnimSpeed = _anim.speed;
        minDashAttackSpeedThr = moveSpeed * 0.5f;
        gameObject.layer = LayerMask.NameToLayer("Player");
    }


    private void Update() {
        //Get jump input
        if (!m_Jump && !_anim.GetBool("Slamming")) {
            m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
        }

        //If not slamming, update where player is facing
        if (!_anim.GetBool("Slamming")) {
            FaceAimDirection();
        }

    }

    void FixedUpdate() {
        Grounded = false;

        // Recharge the doubleJump once one ground
        if (Grounded) m_HasDoubleJump = true;

        Move();
        m_Jump = false;
    }
    void LateUpdate() {
        AdjustAnimationSpeed();

        Debug_UpdateStats();

        UpdateAnimatorParams();
    }

    public override void Flip() {
        base.Flip();

        // if the player is pulling an object, then flipping will move the grabbedObject to that other side as well
        if (grapple.m_Pulling && grapple.grabbedObj) {
            // the offset between the player and the grabbedObj
            var offsetFromGrabbedObj = (grapple.grabbedObj.transform.position - transform.position) * FacingSign;
            // this check prevents the player from moving enemies that are way too far
            if (offsetFromGrabbedObj.magnitude < 2)
                grapple.grabbedObj.transform.position += offsetFromGrabbedObj;
        }
    }

    private void ModifyGravity() {
        Vector2 gravityV2 = Vector2.up * Physics2D.gravity.y * Time.fixedDeltaTime;
        if (rb.velocity.y < 0.1f) { // if player is falling
            rb.velocity += gravityV2 * (fallMultiplier);
        } else if (rb.velocity.y > 0 && !CrossPlatformInputManager.GetButton("Jump")) { // else if player is still moving up
            rb.velocity += gravityV2 * (lowJumpMultiplier);
        }
    }

    public void AddMomentum(float momentumAdded) {
        if (momentumAdded < 0) {
            Debug.LogError("AddMomentum() cannot accept negative value: " + momentumAdded);
            return;
        }
        momentum += momentumAdded;
        Debug.Log("Momentum is now: " + momentumAdded);
    }
    // TODO: when wallclimbing, if input_Y is max (1), player will not slide (stay hanging on the wall)

    /// <summary>
    /// If you want to modify any movements or Rigidbody forces/velocity do that here,
    /// otherwise your changes will be immediately overriden by this method as the velocity is modified directly. </summary>
    private void Move() {
        if (_anim.GetBool("DashAttack"))
            return;
        var crossPlatformInput = new Vector2(CrossPlatformInputManager.GetAxis("Horizontal"), CrossPlatformInputManager.GetAxis("Vertical"));
        if (!BlockMoveInput)
            if (control_AirControl || Grounded)
                _move = crossPlatformInput.x * moveSpeed * momentum;

        // If reached jump peak
        if (Mathf.Approximately(rb.velocity.y, 0) && !Grounded)
            m_ReachedJumpPeak = true;

        // If on the wall
        if (!Grounded && Wallcheck && _move * Mathf.Sign(FacingSign) > 0) {
            // On wall and falling
            if (rb.velocity.y < 0) {
                m_HasDoubleJump = true;

                // limit wallslide speed
                if (rb.velocity.y < -wallSlideSpeedMax) {
                    rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeedMax);
                } else if (crossPlatformInput.y > 0.5f) {
                    print("trying to resist wall slide");
                }
            }
        }

        if (Grounded) {
            m_ReachedJumpPeak = false;
            if (_anim.GetBool("Slamming")) // Stop moving on slam landing
                _move = 0;
        }
        // TODO: on jump buttonUp, if(!m_Jump && Grounded) set rb Y velocity to zero 0

        // Move horizontally
        rb.velocity = /*grapple.m_Flying? Vector2.zero:*/ new Vector2(_move, rb.velocity.y);

        //When to jump
        if (m_Jump) {
            Wallcheck = false;
            if (grapple.m_Flying) grapple.EndGrapple();

            if (Grounded) {
                m_lastGroundSpeed = _move; //Updating lastGroundSpeed
                Jump();
            } else if (Wallcheck) { //If jumping and on the wall:
                // NOTE: the player has to be facing the wall to wallslide/walljump
                Debug.Log("Jump off wall");
                Vector2 jumpOffWallVector = new Vector2(-FacingSign, 1).normalized;
                Debug.DrawLine(transform.position, transform.position + new Vector3(jumpOffWallVector.x, jumpOffWallVector.y));
                rb.velocity = jumpOffWallVector * rb.mass * jumpForce * Time.deltaTime;
                Flip();
                m_Jump = false;
            } else if (control_PlayerCanDoubleJump && m_HasDoubleJump) { // Double jump
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

    /// <summary> Adjusts animation speed when walking, falling, or slamming.</summary>
    protected override void AdjustAnimationSpeed() {
        String clipName = _anim.GetCurrentAnimatorClipInfo(_anim.layerCount - 1)[_anim.layerCount - 1].clip.name;

        if (_anim.GetBool("Slamming") && playerAttack.HasReachedSlamPeak) { //Is using slam_attack and reached peak
            _anim.speed = 0;
        } else if (clipName.Equals("Walk") && Mathf.Abs(rb.velocity.x) >= 0.1) { //walking animation and moving
            _anim.speed = Mathf.Abs(rb.velocity.x * animationSpeedCoeff);
        } else if (clipName.Equals("Air Idle") && Mathf.Abs(rb.velocity.y) >= 0.1) { //Airborn animation and moving
            _anim.speed = Mathf.Abs(Mathf.Log(Mathf.Abs(rb.velocity.y * animationSpeedCoeff * 5f / 8f)));
        } else {
            _anim.speed = m_DefaultAnimSpeed; //Go back to default speed
            playerAttack.HasReachedSlamPeak = false;
        }
        _anim.speed *= momentum;
    }

    public bool CanDashAttack { get { return Mathf.Abs(rb.velocity.x) > minDashAttackSpeedThr * momentum; } }

    protected override void OnCollisionEnter2D(Collision2D collision) {
        base.OnCollisionEnter2D(collision);
    }

    protected override void UpdateAnimatorParams() {
        base.UpdateAnimatorParams();
        _anim.SetBool("Grappling", grapple.m_Flying);

        if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("Attack Dash")) {
            playerAttack.DashAttackClose();
        }
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

    private void Debug_UpdateStats() {
        statsText.text =
              "\nHSpeed: " + rb.velocity.x
            + "\nVSpeed: " + rb.velocity.y
            + "\nHSpeed(Anim): " + _anim.GetFloat("Speed")
            + "\nVSpeed(Anim): " + _anim.GetFloat("VSpeed")
            + "\nlastGroundSpeed: " + m_lastGroundSpeed
            + "\nBlockInput: " + BlockMoveInput
            + "\nDoublejump available? " + m_HasDoubleJump
            + "\nis Wallcheck " + Wallcheck
            + "Grounded: " + Grounded
            + "\nJump: " + m_Jump
            + "\nGrappling: " + grapple.m_Flying;
    }

}
