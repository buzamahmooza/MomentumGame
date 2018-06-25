using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerMove : MonoBehaviour {

#pragma warning disable CS0414 // Member hides inherited member; missing new keyword
    //Parameters
    [SerializeField] private LayerMask floorMask;
    //[SerializeField] private AnimationClip LandAnimation;
    [SerializeField] private bool airJump = false, airControl = true;
    [SerializeField] private float 
        moveSpeed = 200,
        maxSpeed = 20,
        maxVSpeed = 20,
        jumpPower = 200,
        skinWidth = 0.1f,
        groundedDistance = 0.1f,
        animationSpeedFactor = 1.0f,
        airbornDamping = 0.6f;

    //Fields
    [HideInInspector]
    public bool isGrounded = true;
    [HideInInspector]
    public bool jump = false;
    private float animSpeed;
    private float move = 0;
    private float lastGroundSpeed = 0;
    private string rayStats = "";
    Transform groundCheck;
    //private Vector3 startPos;
    private RaycastHit2D hit;
    
    //Components
    new Collider2D collider2D;
    CircleCollider2D circle;
    Rigidbody2D rb2d;
    Animator anim;
    UnityEngine.UI.Text statsText;
    
    private void Awake() {
        if (GameObject.Find("StatsText").GetComponent<UnityEngine.UI.Text>()!=null)
            statsText = GameObject.Find("StatsText").GetComponent<UnityEngine.UI.Text>();
        collider2D = GetComponent<Collider2D>();
        rb2d = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        circle = GetComponent<CircleCollider2D>();
        groundCheck = transform.Find("GroundCheck");
        animSpeed = anim.speed;
    }

    void FixedUpdate() {
        isGrounded = false;
        CheckIfGrounded();

        if (!jump && !anim.GetBool("Slamming"))
            jump = CrossPlatformInputManager.GetButton("Jump") || CrossPlatformInputManager.GetAxis("Vertical") > 0.1;
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene("Main");
        Move();
        MatchAnimationSpeed();
        LimitSpeeds();
        UpdateFlip();
        UpdateStats();
        UpdateAnimatorParams();
        jump = false;
    }

    private void Move() {
        CheckIfGrounded();
        if (airControl || isGrounded)
            move = CrossPlatformInputManager.GetAxis("Horizontal") * moveSpeed;

        //Prevents horizontal movement when landing a slam
        if (anim.GetBool("Slamming") && isGrounded)
            move = 0;

        rb2d.velocity = new Vector2(move, rb2d.velocity.y);

        //Updating lastGroundSpeed
        if(isGrounded)
            lastGroundSpeed = move;

        //When to jump
        if (isGrounded && jump)
                Jump();
    }

    private void MatchAnimationSpeed()
    {
        //Changes animation playback speed only when NOT MOVING and does NOT affect Air_Idle animation
        String clipName = anim.GetCurrentAnimatorClipInfo(anim.layerCount - 1)[anim.layerCount - 1].clip.name.ToString();

        if (anim.GetBool("Slamming") && Player_PunchRobot_Attack.reachedSlamPeak)
        {
            anim.speed = 0;
        }
        //walking
        else if (clipName.Equals("punchrobot_walk_flat") && Mathf.Abs(rb2d.velocity.x) >= 0.1)
        {
            anim.speed = Mathf.Abs(rb2d.velocity.x * animationSpeedFactor);
        }
        //Airborn
        else if (clipName.Equals("Air_Idle") && Mathf.Abs(rb2d.velocity.y) >= 0.1)
        {
            anim.speed = Mathf.Abs(rb2d.velocity.y * animationSpeedFactor / 10.0f);
        }
        //Go back to default speed
        //FIX CONDITIONS
        //else if (!anim.GetBool("Slamming") && anim.speed < 0.1)
        //    anim.speed = animSpeed;
        else
        {
            anim.speed = animSpeed;
            Player_PunchRobot_Attack.reachedSlamPeak = false;
        }
    }

    public bool CheckIfGrounded() {        
        if (airJump || Mathf.Abs(rb2d.velocity.y) < 0.1) {
            isGrounded = true;
            return isGrounded;
        }

        foreach (Collider2D col in Physics2D.OverlapCircleAll(groundCheck.position, circle.radius))
            if (col.gameObject != gameObject && col.gameObject.layer.Equals(floorMask))
                isGrounded = true;

        return (isGrounded);
    }

    private void Jump()
    {
       rb2d.AddForce(Vector2.up * rb2d.gravityScale * jumpPower * Time.deltaTime, ForceMode2D.Impulse);
    }

    private void UpdateAnimatorParams() {        
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("VSpeed", Mathf.Abs(rb2d.velocity.y));
        anim.SetFloat("Speed", Mathf.Abs(move));
    }

    private void UpdateStats()
    {
        statsText.text = "Grounded: " + isGrounded 
                     + "\nJump: " + jump
                     + "\nHSpeed: " + rb2d.velocity.x
                     + "\nVSpeed: " + rb2d.velocity.y
                     + "\nHSpeed(Anim): " + anim.GetFloat("Speed")
                     + "\nVSpeed(Anim): " + anim.GetFloat("VSpeed")
                     + "\nlastGroundSpeed: " + lastGroundSpeed
                     + "\nRaycast object: " + rayStats;
    }

    private void UpdateFlip()
    {
        if(rb2d.velocity.x != 0)
        { 
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(move)*Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
    
    private void LimitSpeeds() {
        //Limits hSpeed
        if (Mathf.Abs(rb2d.velocity.x) > maxSpeed) {
            Debug.Log("Limit HSpeed (" + rb2d.velocity.x + ")");
            float hSpeed = Mathf.Clamp(rb2d.velocity.x, -rb2d.velocity.x, rb2d.velocity.x);
            hSpeed = Mathf.Abs(hSpeed) * Mathf.Sin(rb2d.velocity.x);
            rb2d.velocity = new Vector2(hSpeed, rb2d.velocity.y);
        }
        //Limits vSpeed
        if (Mathf.Abs(rb2d.velocity.y) > maxVSpeed) {
            Debug.Log("Limit VSpeed ("+rb2d.velocity.y+")");
            float vSpeed = Mathf.Clamp(rb2d.velocity.y, -rb2d.velocity.y, rb2d.velocity.y);
            vSpeed = Mathf.Sin(rb2d.velocity.y) * Mathf.Abs (vSpeed);
            rb2d.velocity = new Vector2(rb2d.velocity.x, vSpeed);
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
}
