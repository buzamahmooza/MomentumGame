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
    public bool HasReachedSlamPeak = false;
    [SerializeField] [Range(0, 4f)] private float maxTimeBetweenAttacks = 2f;
    public ComboInstance currentCombo = null;

    [SerializeField] private AudioClip dashAttackSound, slamAttackSound;
    [SerializeField] private GameObject slamExplosionObj = null;

    // This is just to make the members collapsable in the inspector
    [System.Serializable] private struct Hitboxes { public Hitbox punchHitbox, slamHitbox, dashAttackHitbox, uppercutHitbox; }
    [SerializeField] private Hitboxes hitboxes;
    [SerializeField] [Range(0.5f, 10f)] private float explosionRadius = 2;
    [SerializeField] [Range(0f, 10f)] private float upwardModifier = 1;
    [SerializeField] private LayerMask explosionMask;

    private bool slam,
                punch,
                uppercut;

    [SerializeField]
    private float dashAttackSpeedFactor = 1.5f,
                uppercutJumpForce = 1f;
    private float animSpeed = 1;

    // components
    private Animator _anim;
    private PlayerMove playerMove;
    private AudioSource audioSource;
    private Rigidbody2D rb;


    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
        audioSource = GetComponent<AudioSource>();
        _anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (explosionMask == 0) explosionMask = LayerMask.NameToLayer("Enemy") +
                                               LayerMask.NameToLayer("Object");

        if (!hitboxes.punchHitbox) hitboxes.punchHitbox = transform.Find("PunchTrigger").GetComponent<Hitbox>();
        if (!hitboxes.slamHitbox) hitboxes.slamHitbox = transform.Find("SlamTrigger").GetComponent<Hitbox>();
        if (!hitboxes.uppercutHitbox) hitboxes.uppercutHitbox = transform.Find("UppercutTrigger").GetComponent<Hitbox>();
        if (!hitboxes.dashAttackHitbox) hitboxes.dashAttackHitbox = transform.Find("DashAttackTrigger").GetComponent<Hitbox>();
    }

    private void Start()
    {
        print("explosionMask = " + explosionMask.value);
        hitboxes.slamHitbox.enabled = false;
        hitboxes.dashAttackHitbox.enabled = false;
        hitboxes.punchHitbox.enabled = false;
        hitboxes.dashAttackHitbox.enabled = false;
        hitboxes.uppercutHitbox.enabled = false;

        animSpeed = _anim.speed;
        Debug.Assert(hitboxes.slamHitbox && hitboxes.punchHitbox);

        SubscribeToHitEvents();
    }
    // TODO: move the effects (screenshake, hitStop and slomo) here rather than having it in HitBox
    private void SubscribeToHitEvents()
    {
        hitboxes.uppercutHitbox.OnHitEvent += OnHitHandler;
        hitboxes.dashAttackHitbox.OnHitEvent += OnHitHandler;
        hitboxes.punchHitbox.OnHitEvent += OnHitHandler;
        hitboxes.slamHitbox.OnHitEvent += OnHitHandler;
    }
    private void OnHitHandler(GameObject go, float speedMult, bool isFinalBlow)
    {
        if (currentCombo == null || currentCombo.HasEnded)
        {
            currentCombo = new ComboInstance(maxTimeBetweenAttacks);
        }
        currentCombo.IncrementCount();
    }


    private void Update()
    {
        if (currentCombo != null)
        {
            // display combo text
            GameManager.ComboManager.DisplayCombo(currentCombo.HasEnded ? 0 : currentCombo.Count);
            GameManager.ComboManager.comboText.text += "\nCombo time left: " + currentCombo.TimeRemainingBeforeTimeout;

            currentCombo.AddTime(Time.deltaTime);
        }

        // Get input if there is no action to disturb
        if (!uppercut && !punch && !slam)
        {
            uppercut = punch = slam = false;
            var input = new Vector2(CrossPlatformInputManager.GetAxisRaw("Horizontal"), CrossPlatformInputManager.GetAxisRaw("Vertical"));
            var attackDown = Input.GetKeyDown(KeyCode.F) || InputManager.ActiveDevice.Action3.IsPressed;

            if (!_anim.GetBool("DashAttack"))
            {
                // If airborn and pressing down, SlamAttack
                if (AttackInput && input.y <= -0.5f && Mathf.Abs(input.x) <= 0.5)
                {
                    if (playerMove.Grounded || rb.velocity.y >= -0.5f) // if not already falling, jump
                        playerMove.Jump();
                    slam = true;
                }
                // If DashAttack conditions are met, DashAttack!
                else if (attackDown && playerMove.Grounded && playerMove.CanDashAttack && Mathf.Abs(input.x) > 0.1f &&
                         !_anim.GetBool("DashAttack"))
                {
                    playerMove.rb.AddForce(Vector2.right * rb.velocity.x * Time.deltaTime * dashAttackSpeedFactor, ForceMode2D.Impulse);
                    _anim.SetBool("DashAttack", true);
                    // Block input for dashattack animation length
                    StartCoroutine(BlockInput(_anim.GetCurrentAnimatorClipInfo(0).GetLength(0)));
                }
                else if (AttackInput && input.y >= 0.5f && Mathf.Abs(input.x) <= 0.5)
                { //uppercut
                    uppercut = true;
                    _anim.SetTrigger("Uppercut");
                }
                else if (AttackInput)
                {
                    // otherwise just do a boring-ass punch...
                    punch = true;
                }
            }
        }

        UpdateAnimatorParams();

        //When landing a slam on the ground, go back to normal animation speed
        if (_anim.GetBool("Slamming") && playerMove.m_Grounded)
        {
            _anim.speed = animSpeed;
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
        playerMove.BlockMoveInput = true;
        yield return new WaitForSeconds(blockInputDuration);
        playerMove.BlockMoveInput = false;
    }


    // All these are activated during animation

    /// <summary>
    /// Opens the punch interval (the active frames where the hitbox is on)
    /// </summary>
    public void Attack_PunchStart()
    {
        hitboxes.punchHitbox.enabled = true;
    }
    /// <summary>
    /// Closes the punch interval
    /// </summary>
    public void Attack_PunchEnd()
    {
        punch = false;
        hitboxes.punchHitbox.enabled = false;
    }
    public void PunchCompleted()
    {
        punch = false;
        UpdateAnimatorParams();
    }


    public void UppercutStart()
    {
        uppercut = true;
        hitboxes.uppercutHitbox.enabled = true;
        Invoke("UppercutCompleted", 1f);
        rb.AddForce(Vector2.up * uppercutJumpForce, ForceMode2D.Impulse);
    }
    public void UppercutCompleted()
    {
        print("UppercutCompleted()");
        uppercut = false;
        hitboxes.uppercutHitbox.enabled = false;
    }

    public void ReachedSlamPeak()
    {
        HasReachedSlamPeak = true;
        _anim.speed = 0;
        gameObject.layer = LayerMask.NameToLayer("PlayerIgnore");
    }
    public void Attack_Slam()
    {
        hitboxes.slamHitbox.enabled = true;
    }
    public void SlamEnded()
    {
        slam = false;
        _anim.speed = animSpeed;
        hitboxes.slamHitbox.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Player");
        UpdateAnimatorParams();
    }

    public void CreateSlamExplosion()
    {
        Instantiate(slamExplosionObj, hitboxes.slamHitbox.collider2D.bounds.center + Vector3.back, Quaternion.identity);
        float landingSpeed = Mathf.Abs(rb.velocity.y);
        GameManager.CameraShake.DoJitter(0.2f, 0.4f + landingSpeed * 0.3f);

        var explosionPos = transform.position;
        foreach (var hit in Physics2D.OverlapCircleAll(explosionPos, explosionRadius, explosionMask))
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
                new Vector2(playerMove.FacingSign, upwardModifier) * (landingSpeed / distance),
                ForceMode2D.Impulse
            );
        }

    }

    public void DashAttackOpen()
    {
        hitboxes.dashAttackHitbox.enabled = true;
    }
    public void DashAttackClose()
    {
        hitboxes.dashAttackHitbox.enabled = false;
        _anim.SetBool("DashAttack", false);
        punch = false;
        playerMove.BlockMoveInput = false;
    }

    private void UpdateAnimatorParams()
    {
        _anim.SetBool("Punching", punch);
        _anim.SetBool("Slamming", slam);
    }




    // Not in use
    /*
    private bool CheckTrigger(Collider2D collider)
    {
        bool hasCollided = false;

        Bounds trigBounds = collider.bounds;
        Collider2D[] cols = Physics2D.OverlapBoxAll(trigBounds.center, trigBounds.extents,0);

        foreach (Collider2D col in cols)
        {
            bool colliderConditions =
                col.isTrigger != true &&
                col.gameObject != gameObject &&
                col.attachedRigidbody != null;

            if (colliderConditions) {
                Debug.Log(col.gameObject.name + " trigger ACTUALLY collided with other object. Name: " + col.gameObject.name + ", Layer: " + LayerMask.LayerToName(col.gameObject.layer));
                //Vector2 forceDir = Vector2.right * Mathf.Abs(trigBounds.center.x - col.attachedRigidbody.centerOfMass.x) * playerMove.FacingSign;
                
				hasCollided = true;
            }
        }
        return hasCollided;
    }
*/
}