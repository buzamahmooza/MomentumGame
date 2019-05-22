using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

/// <summary>
/// The hitbox class works by disabling and reenabling the script,
/// When active, it's damaging opponents on OnTriggerStay(), rather than using OnTriggerEnter(),
/// and the set of objects damaged by this hitbox session (the session that it was active) will not be hurt again.
/// This prevents from doing damage everysingle active frame.
///
/// The reason OnTriggerEnter can't be used is because it's faulty in the following situation:
/// If the hitbox was activated and then deactivated and the apponent is still overlapping with the hitbox the entire time,
/// then OnTriggerEnter will NOT be called the session even when deactivating and reactivating the hitbox.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    /// <summary>
    /// An event which is called when the hitbox collides
    /// <param name="otherGameObject" type="GameObject"></param>
    /// <param name="speedMult" type="float">the gameObject that was hit</param>
    /// <param name="killedOther" type="bool">if this hit killed the other gameObject</param>
    /// </summary>
    public event Action<GameObject, float, bool> OnHitEvent;

    // assigned in inspector
    /// <summary> X: jitterDuration, Y: jitterIntensity </summary>
    [SerializeField] Vector2 jitter = new Vector2(0.2f, 0.06f);

    [SerializeField] AudioClip attackSound;
    [SerializeField] [Range(1, 1000)] int damageAmount = 25;

    [SerializeField] bool _explosive = false;

    [SerializeField] [Range(0, 1)] float hitStop = 0.25f;
    [SerializeField] [Range(0, 1)] float fisheye = 0;

    /// <summary> the time slowdownFactor, smaller means more slo-mo </summary>
    [SerializeField] [Range(0, 1)] float slomoFactor = 0.4f;

    /// <summary> the amount of effects modification (such as slomo and hitStop) on the finishing blow </summary>
    [SerializeField] [Range(1, 10)] float killCoeff = 1.5f;

    /// <summary>
    /// This influence variable will be used to add custom force to the attack
    /// A positive X value will add force toward where the player is facing (will push the enemy away).
    /// </summary>
    [SerializeField] private Vector2 attackDirection = new Vector2(3f, 3f);

    [SerializeField] private LayerMask layerMask;

    //components
    public Collider2D Collider2D;
    
    private AudioSource m_audioSource;
    private PlayerAttack m_playerAttack;
    private Walker m_walker;

    /// <summary>
    /// The objects that have been already damaged in this hitbox session.
    /// This is helpful to know to prevent hurting the same object more than once 
    /// </summary>
    private readonly HashSet<Collider2D> m_hitsInSession = new HashSet<Collider2D>();

    
    void OnDisable()
    {
        m_hitsInSession.Clear(); // clear the list when ending the session
    }

    void Awake()
    {
        if (layerMask.value == 0)
            layerMask = LayerMask.GetMask("Enemy", "EnemyIgnore", "Object"); //value of 6144
        m_playerAttack = GetComponentInParent<PlayerAttack>();
        Collider2D = GetComponent<Collider2D>();
        
        m_audioSource = GetComponent<AudioSource>();
        if (!m_audioSource) 
            m_audioSource = gameObject.AddComponent<AudioSource>();

        m_walker = GetComponentInParent<Walker>();
        Collider2D.enabled = true;
    }

    void Update()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            Collider2D.bounds.center,
            Collider2D.bounds.size,
            angle: 0,
            layerMask: layerMask
        );
        foreach (Collider2D other in hits)
        {
            // skip self
            if (other.transform == this.transform)
                continue;

            if (m_hitsInSession.Contains(other))
                continue;

            m_hitsInSession.Add(other);

            DamageOther(other);
        }
    }

    private void DamageOther(Collider2D other)
    {
        Health otherHealth = other.gameObject.GetComponent<Health>();
        bool isInLayerMask = Utils.IsInLayerMask(layerMask, other.gameObject.layer);

        bool wasDead = otherHealth && otherHealth.IsDead;

        if (isInLayerMask && other.attachedRigidbody != null)
        {
            Vector2 toTarget = (other.transform.position - this.transform.position).normalized;
            attackDirection.x *= Mathf.Sign(toTarget.x);
            other.attachedRigidbody.AddForce(attackDirection, ForceMode2D.Impulse);

            Vector2 moveDirection = m_walker.Rb.velocity.magnitude > 0
                ? m_walker.Rb.velocity
                : attackDirection;
            
            /**a multiplier that depends on the speed at which the attacker hit*/
            float speedMult = Mathf.Log(
                Utils.FilterMultiplier(Vector2.Dot(moveDirection, attackDirection), 50f)
            );

            if (m_playerAttack.CurrentComboInstance != null)
                speedMult = speedMult * (1 + Mathf.Log(m_playerAttack.CurrentComboInstance.Count));

            // do attack stuff
            if (otherHealth)
                otherHealth.TakeDamage(Mathf.CeilToInt(damageAmount * speedMult), attackDirection);

            bool isFinalBlow = !wasDead && (otherHealth && otherHealth.IsDead);

            // invoke the hit event
            if (otherHealth && !otherHealth.IsDead)
                if (OnHitEvent != null)
                    OnHitEvent(other.gameObject, speedMult, isFinalBlow);


            if (attackSound)
                m_audioSource.PlayOneShot(attackSound);

            if (hitStop > 0)
            {
                float seconds = hitStop * speedMult;

                if (isFinalBlow) seconds *= killCoeff;
                GameComponents.TimeManager.DoHitStop(seconds);
            }

            if (fisheye > 0)
            {
                GameComponents.CameraController.DoFisheye(fisheye);
            }

            if (slomoFactor < 1)
            {
                float theSlowdownFactor = slomoFactor / speedMult;
                if (isFinalBlow) theSlowdownFactor /= killCoeff;
                GameComponents.TimeManager.DoSlowMotion(theSlowdownFactor);
            }

            if (_explosive)
                m_playerAttack.CreateSlamExplosion();

            GameComponents.CameraShake.DoJitter(jitter.x * Mathf.Log(speedMult), jitter.y);
        }
    }
}