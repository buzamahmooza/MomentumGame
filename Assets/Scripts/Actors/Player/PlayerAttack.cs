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
    [SerializeField] [Range(0, 4f)] float maxTimeBetweenAttacks = 2f;
    public ComboInstance CurrentComboInstance = null;

    [SerializeField] AudioClip dashAttackSound, slamAttackSound;
    [SerializeField] GameObject slamExplosionObj = null;

    // This is just to make the members collapsable in the inspector
    [System.Serializable] struct Hitboxes { public Hitbox punchHitbox, slamHitbox, dashAttackHitbox, uppercutHitbox; }
    [SerializeField] Hitboxes _hitboxes;
    [SerializeField] [Range(0.5f, 10f)] float _explosionRadius = 2;
    [SerializeField] [Range(0f, 10f)] float _upwardModifier = 1.7f;
    [SerializeField] LayerMask _explosionMask; // enemy, object

    bool _slam,
        _punch,
        _uppercut;

    [SerializeField]
    float _dashAttackSpeedFactor = 80f,
                _uppercutJumpForce = 1f;
    float _animSpeed = 1;

    // components
    Animator _anim;
    PlayerMove _playerMove;
    Rigidbody2D _rb;


    void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
        GetComponent<AudioSource>();
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();

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

        _animSpeed = _anim.speed;
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
            CurrentComboInstance = new ComboInstance(maxTimeBetweenAttacks);
        }
        CurrentComboInstance.IncrementCount();
    }


    void Update()
    {
        if (CurrentComboInstance != null)
        {
            // display combo text
            GameManager.ComboManager.DisplayCombo(CurrentComboInstance.HasEnded ? 0 : CurrentComboInstance.Count);
            GameManager.ComboManager.comboText.text += "\nCombo time left: " + CurrentComboInstance.TimeRemainingBeforeTimeout;

            CurrentComboInstance.AddTime(Time.deltaTime);
        }

        // Get input if there is no action to disturb
        if (!_uppercut && !_punch && !_slam)
        {
            _uppercut = _punch = _slam = false;
            var input = new Vector2(CrossPlatformInputManager.GetAxisRaw("Horizontal"), CrossPlatformInputManager.GetAxisRaw("Vertical"));
            var attackDown = Input.GetKeyDown(KeyCode.F) || InputManager.ActiveDevice.Action3.IsPressed;

            if (!_anim.GetBool("DashAttack"))
            {
                // If airborn and pressing down, SlamAttack
                if (AttackInput && input.y <= -0.5f && Mathf.Abs(input.x) <= 0.5)
                {
                    if (_playerMove.Grounded) // if not in the air, Jump()
                        _playerMove.Jump();
                    _slam = true;
                }
                // If DashAttack conditions are met, DashAttack!
                else if (attackDown && _playerMove.Grounded && _playerMove.CanDashAttack && Mathf.Abs(input.x) > 0.1f &&
                         !_anim.GetBool("DashAttack"))
                {
                    _playerMove.Rb.AddForce(Vector2.right * _rb.velocity.x * Time.deltaTime * _dashAttackSpeedFactor, ForceMode2D.Impulse);
                    _anim.SetBool("DashAttack", true);
                    // Block input for dashattack animation length
                    StartCoroutine(BlockInput(_anim.GetCurrentAnimatorClipInfo(0).GetLength(0)));
                }
                else if (AttackInput && input.y >= 0.5f && Mathf.Abs(input.x) <= 0.5)
                { //uppercut
                    _uppercut = true;
                    _anim.SetTrigger("Uppercut");
                }
                else if (AttackInput)
                {
                    // otherwise just do a boring-ass punch...
                    _punch = true;
                }
            }
        }

        UpdateAnimatorParams();

        //When landing a slam on the ground, go back to normal animation speed
        if (_anim.GetBool("Slamming") && _playerMove.m_Grounded)
        {
            _anim.speed = _animSpeed;
        }
    }

    private static bool AttackInput
    {
        get { return Input.GetButton("Fire1") || Input.GetKey(KeyCode.Joystick1Button18) || InputManager.ActiveDevice.Action3.IsPressed; }
    }

    /// <summary>
    /// Does not allow player to move for a given time in seconds, to block during an animation use the anim.GetCurrentAnimatorClipInfo(0).GetLength(0) to get the time of the animation
    /// </summary>
    /// <param name="blockInputDuration"></param>
    /// <returns></returns>
    private System.Collections.IEnumerator BlockInput(float blockInputDuration)
    {
        //Debug.Log("BlockInput for "+blockInputDuration+" seconds");
        _playerMove.BlockMoveInput = true;
        yield return new WaitForSeconds(blockInputDuration);
        _playerMove.BlockMoveInput = false;
    }


    // All these are activated during animation

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
        _punch = false;
        _hitboxes.punchHitbox.enabled = false;
    }
    public void PunchCompleted()
    {
        _punch = false;
        UpdateAnimatorParams();
    }


    public void UppercutStart()
    {
        _uppercut = true;
        _hitboxes.uppercutHitbox.enabled = true;
        Invoke("UppercutCompleted", 1f);
        _rb.AddForce(Vector2.up * _uppercutJumpForce, ForceMode2D.Impulse);
    }
    public void UppercutCompleted()
    {
        print("UppercutCompleted()");
        _uppercut = false;
        _hitboxes.uppercutHitbox.enabled = false;
    }

    public void ReachedSlamPeak()
    {
        HasReachedSlamPeak = true;
        _anim.speed = 0;
        gameObject.layer = LayerMask.NameToLayer("PlayerIgnore");
    }
    public void Attack_Slam()
    {
        _hitboxes.slamHitbox.enabled = true;
    }
    public void SlamEnded()
    {
        _slam = false;
        _anim.speed = _animSpeed;
        _hitboxes.slamHitbox.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Player");
        UpdateAnimatorParams();
    }

    public void CreateSlamExplosion()
    {
        Instantiate(slamExplosionObj, _hitboxes.slamHitbox.collider2D.bounds.center + Vector3.back, Quaternion.identity);
        float landingSpeed = Mathf.Abs(_rb.velocity.y);
        GameManager.CameraShake.DoJitter(0.2f, 0.4f + landingSpeed * 0.3f);

        var explosionPos = transform.position;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(explosionPos, _explosionRadius, _explosionMask))
        {
            var otherHealth = hit.gameObject.GetComponent<Health>();
            if (!otherHealth)
            {
                Debug.LogWarning("enemyHealth not found on " + hit.name);
                continue;
            }

            otherHealth.Stun(2);

            float distance = Vector2.Distance(transform.position, hit.gameObject.transform.position);
            var otherRb = hit.GetComponent<Rigidbody2D>();

            if (otherRb) otherRb.AddForce(
                new Vector2(_playerMove.FacingSign, _upwardModifier) * (landingSpeed / distance),
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
        _anim.SetBool("DashAttack", false);
        _punch = false;
        _playerMove.BlockMoveInput = false;
    }

    void UpdateAnimatorParams()
    {
        _anim.SetBool("Punching", _punch);
        _anim.SetBool("Slamming", _slam);
    }
}