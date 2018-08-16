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
    private bool m_attackReady = false;
    private float m_meleeRange = 1f;


    protected override void Awake()
    {
        base.Awake();
        if (!attackHitbox) attackHitbox = GetComponent<EnemyHitbox>() ?? GetComponentInChildren<EnemyHitbox>();
    }

    protected override void Start()
    {
        if (attackHitbox)
            m_meleeRange = attackHitbox.Collider2D.bounds.extents.magnitude +
                         Vector2.Distance(attackHitbox.transform.position, transform.position);
        attackHitbox.Collider2D.enabled = false;
    }

    protected override void Update()
    {
        base.Update();

        // if attack is ready, attack as soon as the player is within melee range
        if (m_attackReady && IsAttacking && Targeting.AimDirection.magnitude < m_meleeRange)
        {
            m_attackReady = false;
            Anim.speed = DefaultAnimSpeed;
        }
    }


    /// <summary>
    /// overriding Attack() so it won't play the attack sound
    /// </summary>
    public override void Attack()
    {
        if (!CanAttack) return;
        if (IsAttacking) return;

        IsAttacking = true;
        Anim.SetTrigger("Attack");
        FaceAimDirection();
    }

    /// <summary>
    /// Jumps to the player direction so he'd get hurt. Just like COD knifing
    /// </summary>
    /// <param name="mult">Defaults to 1</param>
    private void Lundge(float mult = 1f)
    {
        Rb.AddForce(Targeting.AimDirection * lungeForce * mult * Rb.mass, ForceMode2D.Impulse);
    }

    // called by animationEvents

    /// <summary>
    /// Called when the attackIsReady to happen
    /// (for example an enemy raising a sword, when the sword is raised, he's ready to attack)
    /// </summary>
    public void AttackReady()
    {
        m_attackReady = true;
        Anim.speed = 0;
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
        m_attackReady = false;
        Anim.speed = DefaultAnimSpeed;
    }
    public void ActivateHitbox()
    {
        Lundge(0.4f);
        attackHitbox.Collider2D.enabled = true;
        if (attackSound) AudioSource.PlayOneShot(attackSound);
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
        if (IsAttacking)
        {
            Debug.LogWarning("Hitbox was on for long, deactivating");
            CloseHitbox();
        }
    }
    public void CloseHitbox()
    {
        attackHitbox.Collider2D.enabled = false;
        IsAttacking = false;
        m_attackReady = false;
    }
}
