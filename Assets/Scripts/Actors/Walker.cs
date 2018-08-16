using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using InControl;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Targeting))]
[RequireComponent(typeof(Rigidbody2D))]
public class Walker : MonoBehaviour
{
    [SerializeField] protected LayerMask floorMask;
    [SerializeField] protected bool control_AirControl = true;
    [SerializeField] protected bool control_facesAimDirection = true;
    [SerializeField] private bool _neverFlip = false;

    [SerializeField] protected float moveSpeed = 20;
    [SerializeField] protected float jumpForce = 8;

    [SerializeField] [Range(0f, 5f)]
    protected float k_GroundedRadius = 0.08f; // Radius of the overlap circle to determine if grounded

    [SerializeField] protected float AnimationSpeedCoeff = 0.8f;

    [HideInInspector] public bool FacingRight = true;

    /// <summary>
    /// Is a jump pending? Should the character jump on the next update?
    /// </summary>
    [HideInInspector] public bool ToJump = false;

    [HideInInspector] private bool m_grounded = true;

    /// <summary> If set to true, all movement inputs will be ignored </summary>
    [HideInInspector] private bool m_blockMoveInput = false;

    /// <summary> Storing the default animation speed, so that we can go back to it after changing it. </summary>
    protected float DefaultAnimSpeed;

    [HideInInspector] protected float Movement = 0;
    protected float LastGroundSpeed = 0;
    protected bool Climbing = false;

    [SerializeField] protected AudioClip footstepSound;
    [SerializeField] protected Transform m_GroundCheck;

    //Components
    public Rigidbody2D Rb { get; private set; }
    public Health Health { get; private set; }
    protected Animator Anim;
    protected AudioSource AudioSource;
    protected Targeting Targeting;


    /// <summary>
    /// Set component fields:
    ///     Targeting,
    ///     AudioSource,
    ///     Rigidbody2D,
    ///     Animator,
    ///     Health
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


    /// <summary>
    /// If you want to modify any movements or Rigidbody forces/velocity do that here, otherwise your changes will be immediately overriden by this method as the velocity is modified directly
    /// </summary>
    public void Move(Vector2 input, bool jump)
    {
        Move(new Vector2(input.x, Rb.velocity.y));

        //When to jump
        if (jump)
        {
            CallJump();
        }
    }

    public void Move(Vector2 input)
    {
        if (!BlockMoveInput)
            if (control_AirControl || Grounded)
            {
                Movement = input.x * moveSpeed;
            }

        // Move (horizontally only)
        Rb.velocity = input * moveSpeed;
    }

    /// <summary>
    /// This method is called when the Move() method is asked to jump.
    /// This method checks for the conditions of being allowed to jump (such as jumping only when being grounded)
    /// </summary>
    private void CallJump()
    {
        if (Grounded)
        {
            LastGroundSpeed = Movement; //Updating lastGroundSpeed
            Jump();
        }
    }

    public void Jump()
    {
        Rb.AddForce(Vector2.up * Rb.mass * jumpForce, ForceMode2D.Impulse);
    }

    /// <summary> Adjusts animation speed when walking, falling, or slamming.</summary>
    protected virtual void AdjustAnimationSpeed()
    {
        String clipName = Anim.GetCurrentAnimatorClipInfo(Anim.layerCount - 1)[Anim.layerCount - 1].clip.name;

        if (clipName.Equals("Walk") && Mathf.Abs(Rb.velocity.x) >= 0.1)
        {
            //walking animation and moving
            Anim.speed = Mathf.Abs(Rb.velocity.x * AnimationSpeedCoeff);
        }
        else if (clipName.Equals("Air Idle") && Mathf.Abs(Rb.velocity.y) >= 0.1)
        {
            //Airborn animation and moving
            Anim.speed = Mathf.Log(Mathf.Abs(Rb.velocity.y * AnimationSpeedCoeff * 5f / 8f));
        }
        else
        {
            Anim.speed = DefaultAnimSpeed; //Go back to default speed
        }
    }

    public bool Grounded
    {
        get
        {
            m_grounded = false;
            m_grounded = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, floorMask)
                .Any(col => col.gameObject != gameObject && !col.gameObject.transform.IsChildOf(this.transform));

            return m_grounded;
        }
        set { m_grounded = value; }
    }


    /// <summary>
    /// if aiming
    ///     RIGHT: return (1),
    ///     LEFT: return (-1) 
    /// </summary>
    public int FacingSign
    {
        get { return FacingRight ? 1 : -1; }
    }

    /// <summary> If set to true, all movement inputs will be ignored </summary>
    public bool BlockMoveInput
    {
        get { return m_blockMoveInput; }
        set { m_blockMoveInput = value; }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        // Play footstep sound, if player falls fast enough
        if (collision.relativeVelocity.y * Time.deltaTime < 2)
        {
            AudioSource.PlayOneShot(footstepSound, 0.5f);
        }
    }

    /// <summary>
    /// Updating the animatorController parameters: [Grounded, VSpeed, Speed]
    /// this method should be overriden and extended to add any extra animator params.
    /// </summary>
    protected virtual void UpdateAnimatorParams()
    {
        Anim.SetFloat("Speed", Mathf.Abs(Rb.velocity.x));
    }

    public virtual void Flip()
    {
        if (_neverFlip) return;
        FacingRight = !FacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x = -1 * (theScale.x);
        transform.localScale = theScale;

        //Flip healthbar
        Vector3 healthBarScale = Health.healthBar.transform.localScale;
        healthBarScale.x = -healthBarScale.x;
        Health.healthBar.transform.localScale = healthBarScale;
    }

    /// <summary> Uses the <code>FacingRight</code> and m_facesAimDirection to Flip the player's direction </summary>
    protected virtual void FaceAimDirection()
    {
        if (Grounded || control_AirControl)
        {
            if (control_facesAimDirection)
            {
                // If should face the AimDirection
                FaceDirection(Targeting.AimDirection.x);
            }
            else
            {
                FaceDirection(Movement);
            }
        }
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
            Gizmos.DrawWireSphere(m_GroundCheck.position, k_GroundedRadius);
    }
}
//using static Input_AimDirection.AimDirection;