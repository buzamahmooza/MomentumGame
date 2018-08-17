using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using InControl;
using UnityEngine;
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

    [SerializeField] AudioClip dashAttackSound, slamAttackSound;
    [SerializeField] GameObject slamExplosionObj = null;

    // This is just to make the members collapsable in the inspector
    [System.Serializable]
    struct Hitboxes
    {
        public Hitbox punchHitbox, slamHitbox, dashAttackHitbox, uppercutHitbox;
    }

    [SerializeField] Hitboxes _hitboxes;
    [SerializeField] [Range(0.5f, 10f)] float _explosionRadius = 2;
    [SerializeField] [Range(0f, 10f)] float _upwardModifier = 1.7f;
    [SerializeField] LayerMask _explosionMask; // enemy, object

    [SerializeField] float _dashAttackSpeedFactor = 20f;
    [SerializeField] float _uppercutJumpForce = 1f;

    /// <summary>
    /// these booleans control
    /// </summary>
    bool m_slam,
        m_punch,
        m_uppercut;

    float m_animSpeed = 1;

    // components
    Animator m_anim;
    PlayerMove m_playerMove;


    void Awake()
    {
        m_playerMove = GetComponent<PlayerMove>();
        GetComponent<AudioSource>();
        m_anim = GetComponent<Animator>();

        if (_explosionMask == 0) _explosionMask = LayerMask.GetMask("Enemy", "Object");
    }

    void Start()
    {
        print("explosionMask = " + _explosionMask.value);
        _hitboxes.slamHitbox.enabled = false;
        _hitboxes.dashAttackHitbox.enabled = false;
        _hitboxes.punchHitbox.enabled = false;
        _hitboxes.dashAttackHitbox.enabled = false;
        _hitboxes.uppercutHitbox.enabled = false;

        m_animSpeed = m_anim.speed;
        Debug.Assert(_hitboxes.slamHitbox && _hitboxes.punchHitbox);

        SubscribeToHitEvents();
    }

    // TODO: call the effects (screenshake, hitStop and slomo) here rather than having it in Hitbox
    void SubscribeToHitEvents()
    {
        _hitboxes.uppercutHitbox.OnHitEvent += OnHitHandler;
        _hitboxes.dashAttackHitbox.OnHitEvent += OnHitHandler;
        _hitboxes.punchHitbox.OnHitEvent += OnHitHandler;
        _hitboxes.slamHitbox.OnHitEvent += OnHitHandler;
    }

    void OnHitHandler(GameObject go, float speedMult, bool isFinalBlow)
    {
        if (CurrentComboInstance == null || CurrentComboInstance.HasEnded)
        {
            CurrentComboInstance = new ComboInstance(m_maxTimeBetweenAttacks);
        }

        CurrentComboInstance.IncrementCount();
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
            bool attackDown = Input.GetKeyDown(KeyCode.F) || InputManager.ActiveDevice.Action3.IsPressed;

            if (!m_anim.GetBool("DashAttack"))
            {
                // If airborn and pressing down, SlamAttack
                if (AttackInput && input.y <= -0.5f && Mathf.Abs(input.x) <= 0.5)
                {
                    if (m_playerMove.Grounded) // if not in the air, Jump()
                        m_playerMove.Jump();
                    m_slam = true;
                }
                // If DashAttack conditions are met, DashAttack!
                else if (attackDown && m_playerMove.Grounded && m_playerMove.CanDashAttack && Mathf.Abs(input.x) > 0.1f &&
                         !m_anim.GetBool("DashAttack"))
                {
                    m_playerMove.Rb.AddForce(Vector2.right * m_playerMove.Rb.velocity.x * Time.deltaTime * _dashAttackSpeedFactor,
                        ForceMode2D.Impulse);
                    m_anim.SetBool("DashAttack", true);
                    // Block input for dashattack animation length
                    StartCoroutine(BlockInput(m_anim.GetCurrentAnimatorClipInfo(0).GetLength(0)));
                }
                else if (AttackInput && input.y >= 0.5f && Mathf.Abs(input.x) <= 0.5)
                {
                    //uppercut
                    m_uppercut = true;
                    m_anim.SetTrigger("Uppercut");
                }
                else if (AttackInput)
                {
                    // otherwise just do a boring-ass punch...
                    m_punch = true;
                }
            }
        }

        UpdateAnimatorParams();

        //When landing a slam on the ground, go back to normal animation speed
        if (m_anim.GetBool("Slamming") && m_playerMove.Grounded)
        {
            m_anim.speed = m_animSpeed;
        }
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
    public void Attack_PunchStart()
    {
        _hitboxes.punchHitbox.enabled = true;
    }

    /// <summary>
    /// Closes the punch interval
    /// </summary>
    public void Attack_PunchEnd()
    {
        m_punch = false;
        _hitboxes.punchHitbox.enabled = false;
    }

    public void PunchCompleted()
    {
        m_punch = false;
        UpdateAnimatorParams();
    }


    public void UppercutStart()
    {
        m_uppercut = true;
        _hitboxes.uppercutHitbox.enabled = true;
        Invoke("UppercutCompleted", 1f);
        m_playerMove.Rb.AddForce(Vector2.up * _uppercutJumpForce, ForceMode2D.Impulse);
    }

    public void UppercutCompleted()
    {
        print("UppercutCompleted()");
        m_uppercut = false;
        _hitboxes.uppercutHitbox.enabled = false;
    }

    public void ReachedSlamPeak()
    {
        HasReachedSlamPeak = true;
        m_anim.speed = 0;
        gameObject.layer = LayerMask.NameToLayer("PlayerIgnore");
    }

    public void Attack_Slam()
    {
        _hitboxes.slamHitbox.enabled = true;
    }

    public void SlamEnded()
    {
        m_slam = false;
        m_anim.speed = m_animSpeed;
        _hitboxes.slamHitbox.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Player");
        UpdateAnimatorParams();
    }

    public void CreateSlamExplosion()
    {
        Instantiate(slamExplosionObj, _hitboxes.slamHitbox.Collider2D.bounds.center + Vector3.back,
            Quaternion.identity);
        float landingSpeed = Mathf.Abs(m_playerMove.Rb.velocity.y);
        GameComponents.CameraShake.DoJitter(0.2f, 0.4f + landingSpeed * 0.3f);

        Vector3 explosionPos = transform.position;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(explosionPos, _explosionRadius, _explosionMask))
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
                    new Vector2(m_playerMove.FacingSign, _upwardModifier) * (landingSpeed / distance),
                    ForceMode2D.Impulse
                );
        }
    }

    public void DashAttackOpen()
    {
        _hitboxes.dashAttackHitbox.enabled = true;
    }

    public void DashAttackClose()
    {
        _hitboxes.dashAttackHitbox.enabled = false;
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