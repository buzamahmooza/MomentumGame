using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using InControl;
using UnityEngine;
using UnityEngine.Serialization;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(PlayerMove))]
public class PlayerAttack : MonoBehaviour
{
    [HideInInspector] public bool HasReachedSlamPeak = false;

    /// <summary>
    /// The combo timer, combos are canceled if there are no attacks for longer than this time in seconds
    /// </summary>
    [SerializeField] [Range(0, 5f)] float m_maxTimeBetweenAttacks = 3f;

    public ComboInstance CurrentComboInstance = null;

    [FormerlySerializedAs("dashAttackSound")] [SerializeField] AudioClip m_dashAttackSound;
    [FormerlySerializedAs("slamAttackSound")] [SerializeField] AudioClip m_slamAttackSound;
    [FormerlySerializedAs("slamExplosionObj")] [SerializeField] GameObject m_slamExplosionObj = null;

    // This is just to make the members collapsable in the inspector
    [System.Serializable]
    struct Hitboxes
    {
        [FormerlySerializedAs("punchHitbox")] public Hitbox PunchHitbox;
        [FormerlySerializedAs("slamHitbox")] public Hitbox SlamHitbox;
        [FormerlySerializedAs("dashAttackHitbox")] public Hitbox DashAttackHitbox;
        [FormerlySerializedAs("uppercutHitbox")] public Hitbox UppercutHitbox;
    }

    [FormerlySerializedAs("_hitboxes")] [SerializeField] Hitboxes m_hitboxes;
    [FormerlySerializedAs("_explosionRadius")] [SerializeField] [Range(0.5f, 10f)] float m_explosionRadius = 2f;
    [FormerlySerializedAs("_upwardModifier")] [SerializeField] [Range(0f, 10f)] float m_upwardModifier = 1.7f;
    [FormerlySerializedAs("_explosionMask")] [SerializeField] LayerMask m_explosionMask; // enemy, object

    [FormerlySerializedAs("_dashAttackSpeedFactor")] [SerializeField] float m_dashAttackSpeedFactor = 20f;
    [FormerlySerializedAs("_uppercutJumpForce")] [SerializeField] float m_uppercutJumpForce = 1f;

    /// these booleans control
    bool m_slam = false,
        m_punch = false,
        m_uppercut = false;

    float m_animSpeed = 1;

    // components
    Animator m_anim;
    PlayerMove m_playerMove;
    [SerializeField] [Range(0, 50)] private float m_attackLunge = 1f;
    [FormerlySerializedAs("hitSettleMult")] [SerializeField] private float m_hitSettleMult = 5f;


    void Awake()
    {
        m_playerMove = GetComponent<PlayerMove>();
        GetComponent<AudioSource>();
        m_anim = GetComponent<Animator>();

        if (m_explosionMask == 0) m_explosionMask = LayerMask.GetMask("Enemy", "Object");
    }

    void Start()
    {
        m_hitboxes.SlamHitbox.enabled = false;
        m_hitboxes.DashAttackHitbox.enabled = false;
        m_hitboxes.PunchHitbox.enabled = false;
        m_hitboxes.DashAttackHitbox.enabled = false;
        m_hitboxes.UppercutHitbox.enabled = false;

        m_animSpeed = m_anim.speed;
        Debug.Assert(m_hitboxes.SlamHitbox && m_hitboxes.PunchHitbox);

        SubscribeToHitEvents();
    }

    // TODO: call the effects (screenshake, hitStop and slomo) here rather than having it in Hitbox
    void SubscribeToHitEvents()
    {
        m_hitboxes.UppercutHitbox.OnHitEvent += OnHitHandler;
        m_hitboxes.DashAttackHitbox.OnHitEvent += OnHitHandler;
        m_hitboxes.PunchHitbox.OnHitEvent += OnHitHandler;
        m_hitboxes.SlamHitbox.OnHitEvent += OnHitHandler;
    }

    void OnHitHandler(GameObject go, float speedMult, bool isFinalBlow)
    {
        if (CurrentComboInstance == null || CurrentComboInstance.HasEnded)
        {
            CurrentComboInstance = new ComboInstance(m_maxTimeBetweenAttacks);
        }

        CurrentComboInstance.IncrementCount();


        FreezeFall();
    }


    void Update()
    {
        if (CurrentComboInstance != null)
        {
            // display combo text
            GameComponents.ComboManager.DisplayCombo(CurrentComboInstance.HasEnded ? 0 : CurrentComboInstance.Count);
            GameComponents.ComboManager.comboText.text +=
                "\nCombo time left: " + CurrentComboInstance.TimeRemainingBeforeTimeout;

            CurrentComboInstance.AddTime(Time.deltaTime);
        }

        // Get input if there is no action to disturb
        if (!m_uppercut && !m_punch && !m_slam)
        {
            m_uppercut = m_punch = m_slam = false;
            Vector2 input = new Vector2(CrossPlatformInputManager.GetAxisRaw("Horizontal"),
                CrossPlatformInputManager.GetAxisRaw("Vertical"));
            bool isAttackInputDown = Input.GetKeyDown(KeyCode.F) || InputManager.ActiveDevice.Action3.IsPressed;

            if (!m_anim.GetBool("DashAttack"))
            {
                // If airborn and pressing down, SlamAttack
                if (AttackInput && input.y <= -0.5f && Mathf.Abs(input.x) <= 0.5)
                {
                    if (m_playerMove.Grounded) // if not in the air, Jump()
                        m_playerMove.Jump();
                    m_slam = true;
                    UpdateAnimatorParams();
                }
                // If DashAttack conditions are met, DashAttack!
                else if (isAttackInputDown && m_playerMove.Grounded && m_playerMove.CanDashAttack &&
                         Mathf.Abs(input.x) > 0.1f && !m_anim.GetBool("DashAttack"))
                {
                    m_playerMove.Rb.AddForce(
                        Vector2.right * m_playerMove.Rb.velocity.x * Time.deltaTime * m_dashAttackSpeedFactor,
                        ForceMode2D.Impulse);
                    m_anim.SetBool("DashAttack", true);
                    // Block input for dashattack animation length
                    StartCoroutine(BlockInput(m_anim.GetCurrentAnimatorClipInfo(0).GetLength(0)));
                    UpdateAnimatorParams();

                    if (m_playerMove.Rb.velocity.y < 1)
                        FreezeFall();
                }
                // uppercut
                else if (AttackInput && input.y >= 0.5f && Mathf.Abs(input.x) <= 0.5)
                {
                    m_uppercut = true;
                    m_anim.SetTrigger("Uppercut");
                    UpdateAnimatorParams();

                    FreezeFall();
                    m_playerMove.Rb.AddForce(Vector2.up);
                }
                else if (isAttackInputDown)
                {
                    // otherwise just do a boring-ass punch...
                    m_punch = true;
                    StartCoroutine(BlockInput(1f));
                    UpdateAnimatorParams();

                    if (m_playerMove.Rb.velocity.y < 1)
                    {
//                        FreezeFall();
//                        Invoke("UnfreezeFall", 1f);
                    }
                }
            }
        }

        //When landing a slam on the ground, go back to normal animation speed
        if (m_anim.GetBool("Slamming") && m_playerMove.Grounded)
        {
            m_anim.speed = m_animSpeed;
        }
    }

    /// stops the player from falling and floats in the air for a bit
    private void FreezeFall()
    {
        // stop the player from falling, (just like dishwasher vampire and Salt&Sanctuary)
        m_playerMove.Rb.gravityScale = 0;
        m_playerMove.Rb.velocity = new Vector2(m_playerMove.Rb.velocity.x, 0);
    }
    private void UnfreezeFall()
    {
        m_playerMove.Rb.gravityScale = 1.2f; // todo: don't hardcode
        m_playerMove.Rb.velocity = new Vector2(m_playerMove.Rb.velocity.x, 0);
    }

    private static bool AttackInput
    {
        get
        {
            return Input.GetButton("Fire1") || Input.GetKey(KeyCode.Joystick1Button18) ||
                   InputManager.ActiveDevice.Action3.IsPressed;
        }
    }

    /// <summary>
    /// Does not allow player to move for a given time in seconds.
    /// Hint: To block during an animation use the anim.GetCurrentAnimatorClipInfo(0).GetLength(0) to get the time of the animation
    /// </summary>
    /// <param name="blockInputDuration"></param>
    /// <returns></returns>
    private IEnumerator BlockInput(float blockInputDuration)
    {
        //Debug.Log("BlockInput for "+blockInputDuration+" seconds");
        m_playerMove.BlockMoveInput = true;
        yield return new WaitForSeconds(blockInputDuration);
        m_playerMove.BlockMoveInput = false;
    }


    // All the below are activated during animation

    /// <summary>
    /// Opens the punch interval (the active frames where the hitbox is on)
    /// </summary>
    public void PunchOpenHitbox()
    {
        m_hitboxes.PunchHitbox.enabled = true;
        //todo: fix this, playermove Move() is overriding the speed
        m_playerMove.Rb.AddForce(Vector2.right * m_playerMove.FacingSign * m_attackLunge,
            ForceMode2D.Impulse);
    }

    /// <summary>
    /// Closes the punch interval
    /// </summary>
    public void PunchCloseHitbox()
    {
        m_punch = false;
        m_hitboxes.PunchHitbox.enabled = false;
    }

    public void PunchCompleted()
    {
        m_punch = false;
        UpdateAnimatorParams();
    }


    public void UppercutStart()
    {
        m_uppercut = true;
        m_hitboxes.UppercutHitbox.enabled = true;
        Invoke("UppercutCompleted", 1f);
        m_playerMove.Rb.AddForce(Vector2.up * m_uppercutJumpForce, ForceMode2D.Impulse);
    }

    public void UppercutCompleted()
    {
        print("UppercutCompleted()");
        m_uppercut = false;
        m_hitboxes.UppercutHitbox.enabled = false;
    }

    public void ReachedSlamPeak()
    {
        HasReachedSlamPeak = true;
        m_anim.speed = 0;
        gameObject.layer = LayerMask.NameToLayer("PlayerIgnore");

        CheckIfLandedAndEndSlam();
    }

    public void Attack_Slam()
    {
        m_hitboxes.SlamHitbox.enabled = true;
    }

    private void CheckIfLandedAndEndSlam()
    {
        if (m_playerMove.Grounded)
        {
            Attack_Slam();
            CreateSlamExplosion();
            SlamEnded();
        }
        else Invoke("CheckIfLandedAndEndSlam", 0.5f);
    }

    public void SlamEnded()
    {
        m_slam = false;
        m_anim.speed = m_animSpeed;
        m_hitboxes.SlamHitbox.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Player");
        UpdateAnimatorParams();
    }

    public void CreateSlamExplosion()
    {
        Instantiate(m_slamExplosionObj, m_hitboxes.SlamHitbox.Collider2D.bounds.center + Vector3.back,
            Quaternion.identity);
        float landingSpeed = Mathf.Abs(m_playerMove.Rb.velocity.y);
        GameComponents.CameraShake.DoJitter(0.2f, 0.4f + landingSpeed * 0.3f);

        Vector3 explosionPos = transform.position;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(explosionPos, m_explosionRadius, m_explosionMask))
        {
            Health otherHealth = hit.gameObject.GetComponent<Health>();
            if (!otherHealth)
            {
                Debug.LogWarning("enemyHealth not found on " + hit.name);
                continue;
            }

            otherHealth.Stun(2);

            float distance = Vector2.Distance(transform.position, hit.gameObject.transform.position);
            Rigidbody2D otherRb = hit.GetComponent<Rigidbody2D>();

            if (otherRb)
                otherRb.AddForce(
                    new Vector2(m_playerMove.FacingSign, m_upwardModifier) * (landingSpeed / distance),
                    ForceMode2D.Impulse
                );
        }
    }

    public void DashAttackOpen()
    {
        m_hitboxes.DashAttackHitbox.enabled = true;
    }

    public void DashAttackClose()
    {
        m_hitboxes.DashAttackHitbox.enabled = false;
        m_anim.SetBool("DashAttack", false);
        m_punch = false;
        m_playerMove.BlockMoveInput = false;
    }

    private void UpdateAnimatorParams()
    {
        m_anim.SetBool("Punching", m_punch);
        m_anim.SetBool("Slamming", m_slam);
    }
}