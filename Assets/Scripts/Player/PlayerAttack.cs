using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(PlayerMove))]
public class PlayerAttack : MonoBehaviour
{
    public bool HasReachedSlamPeak = false;

    [SerializeField] private float slamLundgeVelocity = 5;
    [SerializeField] private AudioClip punchAttackSound, dashAttackSound, slamAttackSound;
    [SerializeField] private GameObject slamExplosionObj;
    [SerializeField] private LayerMask punchLayer;

    [SerializeField] private int slamDamage = 40, punchDamage = 25, dashDamage = 20;
    private bool slamming = false, punching = false;

    // This is just to make the members collapsable in the inspector
    [Serializable] private struct Triggers { public BoxCollider2D punchTrigger, slamTrigger, dashAttackTrigger; }
    [SerializeField] private Triggers triggers;
    [SerializeField]
    [Range(0.5f, 10)]
    private float explosionRadius = 2,
    upwardModifier = 1;
    [SerializeField] private LayerMask explosionMask;

    //BoxCollider2D punchTrigger, slamTrigger, dashAttackTrigger;
    private Animator _anim;
    private PlayerMove playerMove;
    private AudioSource audioSource;
    private Rigidbody2D rb;

    private float animSpeed = 1;
    [SerializeField]
    private float dashAttackSpeedFactor = 1.5f;

    private void Awake() {
        playerMove = GetComponent<PlayerMove>();
        audioSource = GetComponent<AudioSource>();
        _anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (!triggers.punchTrigger) triggers.punchTrigger = transform.Find("PunchTrigger").GetComponent<BoxCollider2D>();
        if (!triggers.slamTrigger) triggers.slamTrigger = transform.Find("SlamTrigger").GetComponent<BoxCollider2D>();
    }

    private void Start() {
        triggers.slamTrigger.enabled = false;
        triggers.dashAttackTrigger.enabled = false;
        triggers.punchTrigger.enabled = false;
        animSpeed = _anim.speed;
        Debug.Assert(triggers.slamTrigger && triggers.punchTrigger);
    }

    private void Update() {
        // Get input if there is no action to disturb
        if (!(punching || slamming)) {
            if (AttackInput) {
                //AttackAutoSelector();
                SelectAttackBasedOnInput();
            }
        }

        UpdateAnimatorParams();

        //When landing a slam on the ground, go back to normal animation speed
        if (_anim.GetBool("Slamming") && playerMove.m_Grounded)
            _anim.speed = animSpeed;
    }

    private static bool AttackInput {
        get { return Input.GetButton("Fire1") || Input.GetKeyDown(KeyCode.Joystick1Button18); }
    }

    private void SelectAttackBasedOnInput() {
        punching = slamming = false;
        if (_anim.GetBool("DashAttack")) return;

        // If airborn and pressing down, SlamAttack
        if (CrossPlatformInputManager.GetAxisRaw("Vertical") < 0) {
            slamming = true;
        }
        // If DashAttack conditions are met, DashAttack!
        else if (playerMove.Grounded &&
                playerMove.CanDashAttack &&
                (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Joystick1Button18)) &&
                Mathf.Abs(CrossPlatformInputManager.GetAxisRaw("Horizontal")) > 0.1f &&
                !_anim.GetBool("DashAttack")
                ) {
            playerMove.rb.velocity = new Vector2(rb.velocity.x * dashAttackSpeedFactor, playerMove.rb.velocity.y);
            _anim.SetBool("DashAttack", true);
            // Block input for dashattack animation length
            StartCoroutine(BlockInput(_anim.GetCurrentAnimatorClipInfo(0).GetLength(0)));
        } else  // otherwise just do a boring-ass punch...
          {
            punching = true;
        }

        UpdateAnimatorParams();
    }
    /// <summary> @Deprecated </summary>
    private void SelectAttackBasedOnPlayerSituation() {
        punching = slamming = false;

        // If airborn and pressing down, SlamAttack
        if (!playerMove.Grounded /*&& CrossPlatformInputManager.GetAxisRaw("Vertical") < 0*/)
            slamming = true;
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
                punching = true;
            }
        }

        UpdateAnimatorParams();
    }

    public void DoAttackStuff(Collider2D col, Collider2D other) {
        int damageAmount = 0;
        // Punch
        if (col.Equals(triggers.punchTrigger)) {
            audioSource.PlayOneShot(punchAttackSound);
            damageAmount = punchDamage;
        }// Slam
        else if (col.Equals(triggers.slamTrigger)) {
            CreateSlamExplosion();
            GameManager.TimeManager.DoSlowMotion();
            audioSource.PlayOneShot(slamAttackSound);
            damageAmount = slamDamage;
        }// Dash
        else if (col.Equals(triggers.dashAttackTrigger)) {
            audioSource.PlayOneShot(dashAttackSound);
            damageAmount = dashDamage;
        } else {
            Debug.LogWarning("The attack collider is not one of the known colliders");
        }

        HealthScript health_Script = other.gameObject.GetComponent<HealthScript>();
        if (health_Script != null)
            health_Script.TakeDamage(damageAmount);
    }

    /// <summary>
    /// Does not allow player to move for a given time in seconds, to block during an animation use the anim.GetCurrentAnimatorClipInfo(0).GetLength(0) to get the time of the animation
    /// </summary>
    /// <param name="blockInputDuration"></param>
    /// <returns></returns>
    private System.Collections.IEnumerator BlockInput(float blockInputDuration) {
        //Debug.Log("BlockInput for "+blockInputDuration+" seconds");
        playerMove.blockMoveInput = true;
        yield return new WaitForSeconds(blockInputDuration);
        playerMove.blockMoveInput = false;
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
        punching = false;
        triggers.punchTrigger.enabled = false;
    }
    public void PunchCompleted() {
        punching = false;
        UpdateAnimatorParams();
    }

    public void ReachedSlamPeak() {
        HasReachedSlamPeak = true;
        _anim.speed = 0;
    }
    public void Attack_Slam() {
        triggers.slamTrigger.enabled = true;
    }
    public void SlamEnded() {
        slamming = false;
        _anim.speed = animSpeed;
        triggers.slamTrigger.enabled = false;
        UpdateAnimatorParams();
    }

    private void CreateSlamExplosion() {
        Instantiate(slamExplosionObj, triggers.slamTrigger.bounds.center + Vector3.back, Quaternion.identity);
        float landingSpeed = -rb.velocity.y;
        GameManager.CameraShake.DoJitter(0.2f, 0.4f + landingSpeed * 0.3f);

        Vector3 explosionPos = transform.position;
        foreach (var hit in Physics2D.OverlapCircleAll((Vector2)explosionPos, explosionRadius, explosionMask)) {
            print(hit.name + " is supposed to take damage");

            Enemy_Health enemyHealth = hit.gameObject.GetComponent<Enemy_Health>();
            if (!enemyHealth) {
                print("enemyHealth not found on " + hit.name);
                continue;
            }

            Rigidbody2D otherRb = hit.GetComponent<Rigidbody2D>();

            StartCoroutine(enemyHealth.EnumStun(2));

            float distance = Vector2.Distance(transform.position, hit.gameObject.transform.position);
            if (otherRb != null)
                otherRb.AddForce(
                        new Vector2(playerMove.FacingRightSign, upwardModifier) * (landingSpeed / distance),
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
        punching = false;
        playerMove.blockMoveInput = false;
    }

    private void UpdateAnimatorParams() {
        _anim.SetBool("Punching", punching);
        _anim.SetBool("Slamming", slamming);
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
                //Vector2 forceDir = Vector2.right * Mathf.Abs(trigBounds.center.x - col.attachedRigidbody.centerOfMass.x) * playerMove.FacingRightSign;
                
				hasCollided = true;
            }
        }
        return hasCollided;
    }
*/
}
