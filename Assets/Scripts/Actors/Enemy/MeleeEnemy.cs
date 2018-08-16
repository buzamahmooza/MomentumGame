using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The enemy will lunge once the attack is ready,
/// and then another tiny lunge when the attackOpens just to get closer
/// </summary>
public class MeleeEnemy : Enemy
{
    [SerializeField] private EnemyHitbox attackHitbox;
    [SerializeField] [Range(0, 10)] private float lungeForce = 2f;
    private bool attackReady = false;
    private float meleeRange = 1f;


    protected override void Awake()
    {
        base.Awake();
        if (!attackHitbox) attackHitbox = GetComponent<EnemyHitbox>() ?? GetComponentInChildren<EnemyHitbox>();
    }

    protected void Start()
    {
        if (attackHitbox)
            meleeRange = attackHitbox.collider2D.bounds.extents.magnitude +
                         Vector2.Distance(attackHitbox.transform.position, transform.position);
        attackHitbox.collider2D.enabled = false;
    }

    protected override void Update()
    {
        base.Update();

        // if attack is ready, attack as soon as the player is within melee range
        if (attackReady && m_Attacking && targeting.AimDirection.magnitude < meleeRange)
        {
            attackReady = false;
            _anim.speed = m_DefaultAnimSpeed;
        }
    }


    /// <summary>
    /// overriding Attack() so it won't play the attack sound
    /// </summary>
    public override void Attack()
    {
        if (!m_CanAttack) return;
        if (m_Attacking) return;

        m_Attacking = true;
        _anim.SetTrigger("Attack");
        FaceAimDirection();
    }

    /// <summary>
    /// Jumps to the player direction so he'd get hurt. Just like COD knifing
    /// </summary>
    /// <param name="mult">Defaults to 1</param>
    private void Lundge(float mult = 1f)
    {
        Rb.AddForce(targeting.AimDirection * lungeForce * mult * Rb.mass, ForceMode2D.Impulse);
    }

    // called by animationEvents

    /// <summary>
    /// Called when the attackIsReady to happen
    /// (for example an enemy raising a sword, when the sword is raised, he's ready to attack)
    /// </summary>
    public void AttackReady()
    {
        attackReady = true;
        _anim.speed = 0;
        Lundge();
        StartCoroutine(AttackReadyTimeout());
    }
    /// <summary>
    /// This will get the enemy unstuck from the position of holding his waepon up,
    /// waiting for the player to be in range.
    /// The enemy will only wait so long.
    /// </summary>
    /// <returns></returns>
    IEnumerator AttackReadyTimeout(float seconds = 0.6f)
    {
        yield return new WaitForSeconds(seconds);
        attackReady = false;
        _anim.speed = m_DefaultAnimSpeed;
    }
    public void ActivateHitbox()
    {
        Lundge(0.4f);
        attackHitbox.collider2D.enabled = true;
        if (attackSound) audioSource.PlayOneShot(attackSound);
        StartCoroutine(Safety_DeactivateHitbox());
    }
    /// <summary>
    /// deactivates hitbox in case it was on for too long for some reason
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    IEnumerator Safety_DeactivateHitbox(float seconds = 1f)
    {
        yield return new WaitForSeconds(seconds);
        if (m_Attacking)
        {
            Debug.LogWarning("Hitbox was on for long, deactivating");
            CloseHitbox();
        }
    }
    public void CloseHitbox()
    {
        attackHitbox.collider2D.enabled = false;
        m_Attacking = false;
        attackReady = false;
    }
}
