using System;
using InControl;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(PlayerMove))]
public class PlayerAttack : MonoBehaviour
{
    public bool HasReachedSlamPeak = false;

    [SerializeField] private AudioClip dashAttackSound, slamAttackSound;
    [SerializeField] private GameObject slamExplosionObj;
    [SerializeField] private LayerMask punchLayer;

    private bool slam = false,
        punch = false;

    // This is just to make the members collapsable in the inspector
    [Serializable] private struct Triggers { public Collider2D punchTrigger, slamTrigger, dashAttackTrigger, uppercutTrigger; }
    [SerializeField] private Triggers triggers;
    [SerializeField] [Range(0.5f, 10f)] private float explosionRadius = 2;
    [SerializeField] [Range(0f, 10f)] private float upwardModifier = 1;
    [SerializeField] private LayerMask explosionMask;

    //BoxCollider2D punchTrigger, slamTrigger, dashAttackTrigger;
    private Animator _anim;
    private PlayerMove playerMove;
    private AudioSource audioSource;
    private Rigidbody2D rb;

    private float animSpeed = 1;
    [SerializeField]
    private float dashAttackSpeedFactor = 1.5f;

    private bool uppercut = false;
    [SerializeField] private float uppercutForce = 1f;


    private void Awake() {
        playerMove = GetComponent<PlayerMove>();
        audioSource = GetComponent<AudioSource>();
        _anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (explosionMask == 0) explosionMask = LayerMask.NameToLayer("Enemy") +
                                               LayerMask.NameToLayer("Object");

        if (!triggers.punchTrigger) triggers.punchTrigger = transform.Find("PunchTrigger").GetComponent<Collider2D>();
        if (!triggers.slamTrigger) triggers.slamTrigger = transform.Find("SlamTrigger").GetComponent<Collider2D>();
        if (!triggers.uppercutTrigger) triggers.uppercutTrigger = transform.Find("UppercutTrigger").GetComponent<Collider2D>();
        if (!triggers.dashAttackTrigger) triggers.dashAttackTrigger = transform.Find("DashAttackTrigger").GetComponent<Collider2D>();

    }

    private void Start() {
        print("explosionMask = " + explosionMask.value);
        triggers.slamTrigger.enabled = false;
        triggers.dashAttackTrigger.enabled = false;
        triggers.punchTrigger.enabled = false;
        triggers.dashAttackTrigger.enabled = false;

        animSpeed = _anim.speed;
        Debug.Assert(triggers.slamTrigger && triggers.punchTrigger);
    }

    private void Update() {
        if (false) Debug.Log(
            "\n A = " + InputManager.ActiveDevice.Action1.IsPressed +
            "\n B =" + InputManager.ActiveDevice.Action2.IsPressed +
            "\n X =" + InputManager.ActiveDevice.Action3.IsPressed +
            "\n Y =" + InputManager.ActiveDevice.Action4.IsPressed
        );

        // Get input if there is no action to disturb
        if (!uppercut && !punch && !slam) {
            uppercut = punch = slam = false;
            var input = new Vector2(CrossPlatformInputManager.GetAxisRaw("Horizontal"), CrossPlatformInputManager.GetAxisRaw("Vertical"));
            var attackDown = Input.GetKeyDown(KeyCode.F) || InputManager.ActiveDevice.Action3.IsPressed;

            if (!_anim.GetBool("DashAttack")) {
                // If airborn and pressing down, SlamAttack
                if (AttackInput && input.y < -0.5f && Mathf.Abs(input.x) < 0.5) {
                    slam = true;
                }
                // If DashAttack conditions are met, DashAttack!
                else if (attackDown && playerMove.Grounded && playerMove.CanDashAttack && Mathf.Abs(input.x) > 0.1f &&
                         !_anim.GetBool("DashAttack")) {
                    playerMove.rb.AddForce(Vector2.right * rb.velocity.x * Time.deltaTime * dashAttackSpeedFactor, ForceMode2D.Impulse);
                    _anim.SetBool("DashAttack", true);
                    // Block input for dashattack animation length
                    StartCoroutine(BlockInput(_anim.GetCurrentAnimatorClipInfo(0).GetLength(0)));
                } else if (AttackInput && input.y > 0.5f && Mathf.Abs(input.x) < 0.5) { //uppercut
                    uppercut = true;
                    _anim.SetTrigger("Uppercut");
                } else if (AttackInput) {
                    // otherwise just do a boring-ass punch...
                    punch = true;
                }
            }
        }

        UpdateAnimatorParams();

        //When landing a slam on the ground, go back to normal animation speed
        if (_anim.GetBool("Slamming") && playerMove.m_Grounded)
            _anim.speed = animSpeed;
    }

    private static bool AttackInput {
        get { return Input.GetButton("Fire1") || Input.GetKey(KeyCode.Joystick1Button18) || InputManager.ActiveDevice.Action3.IsPressed; }
    }

    /// <summary> @Deprecated </summary>
    private void SelectAttackBasedOnPlayerSituation() {
        punch = slam = false;

        // If airborn and pressing down, SlamAttack
        if (!playerMove.Grounded /*&& CrossPlatformInputManager.GetAxisRaw("Vertical") < 0*/)
            slam = true;
        // If grounded
        else if (playerMove.Grounded) {
            // If DashAttack conditions are met, DashAttack!
            if (playerMove.CanDashAttack && !_anim.GetBool("DashAttack")) {
                playerMove.rb.velocity = new Vector2(rb.velocity.x * dashAttackSpeedFactor, playerMove.rb.velocity.y);
                _anim.SetBool("DashAttack", true);
                // Block input for dashattack animation length
                StartCoroutine(BlockInput(_anim.GetCurrentAnimatorClipInfo(0).GetLength(0)));
            }
            // otherwise just do a boring-ass punch...
            else if (_anim.GetFloat("Speed") < 0.1f) {
                punch = true;
            }
        }

        UpdateAnimatorParams();
    }

    /// <summary>
    /// Does not allow player to move for a given time in seconds, to block during an animation use the anim.GetCurrentAnimatorClipInfo(0).GetLength(0) to get the time of the animation
    /// </summary>
    /// <param name="blockInputDuration"></param>
    /// <returns></returns>
    private System.Collections.IEnumerator BlockInput(float blockInputDuration) {
        //Debug.Log("BlockInput for "+blockInputDuration+" seconds");
        playerMove.BlockMoveInput = true;
        yield return new WaitForSeconds(blockInputDuration);
        playerMove.BlockMoveInput = false;
    }


    // All these are activated during animation

    /// <summary>
    /// Opens the punch interval (the active frames where the hitbox is on)
    /// </summary>
    public void Attack_PunchStart() {
        triggers.punchTrigger.enabled = true;
    }
    /// <summary>
    /// Closes the punch interval
    /// </summary>
    public void Attack_PunchEnd() {
        punch = false;
        triggers.punchTrigger.enabled = false;
    }
    public void PunchCompleted() {
        punch = false;
        UpdateAnimatorParams();
    }


    public void UppercutStart() {
        triggers.uppercutTrigger.enabled = true;
        rb.AddForce(Vector2.up * uppercutForce, ForceMode2D.Impulse);
    }
    public void UppercutCompleted() {
        print("UppercutCompleted()");
        uppercut = false;
        triggers.uppercutTrigger.enabled = false;
    }

    public void ReachedSlamPeak() {
        HasReachedSlamPeak = true;
        _anim.speed = 0;
        gameObject.layer = LayerMask.NameToLayer("PlayerIgnore");
    }
    public void Attack_Slam() {
        triggers.slamTrigger.enabled = true;
    }
    public void SlamEnded() {
        slam = false;
        _anim.speed = animSpeed;
        triggers.slamTrigger.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Player");
        UpdateAnimatorParams();
    }

    public void CreateSlamExplosion() {
        Instantiate(slamExplosionObj, triggers.slamTrigger.bounds.center + Vector3.back, Quaternion.identity);
        float landingSpeed = Mathf.Abs(rb.velocity.y);
        GameManager.CameraShake.DoJitter(0.2f, 0.4f + landingSpeed * 0.3f);

        var explosionPos = transform.position;
        foreach (var hit in Physics2D.OverlapCircleAll(explosionPos, explosionRadius, explosionMask)) {
            var otherHealth = hit.gameObject.GetComponent<Health>();
            if (!otherHealth) {
                Debug.LogWarning("enemyHealth not found on " + hit.name);
                continue;
            }

            otherHealth.Stun(2);

            float distance = Vector2.Distance(transform.position, hit.gameObject.transform.position);
            Rigidbody2D otherRb = hit.GetComponent<Rigidbody2D>();

            if (otherRb) otherRb.AddForce(
                new Vector2(playerMove.FacingSign, upwardModifier) * (landingSpeed / distance),
                ForceMode2D.Impulse
            );
        }

    }

    public void DashAttackOpen() {
        triggers.dashAttackTrigger.enabled = true;
    }
    public void DashAttackClose() {
        triggers.dashAttackTrigger.enabled = false;
        _anim.SetBool("DashAttack", false);
        punch = false;
        playerMove.BlockMoveInput = false;
    }

    private void UpdateAnimatorParams() {
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
