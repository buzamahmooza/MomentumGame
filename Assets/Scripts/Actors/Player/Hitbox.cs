using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

/// <summary>
/// The hitbox class works by enabling and disabling the Collider2D
/// (disabling/enabling the script will also enable/disable the collider).
/// Rather than using OnTriggerEnter(), OnTriggerStay() is used,
/// and a list of objects that have already been damaged this hitbox session (the session that it was active).
/// This prevents from doing damage everysingle active frame.
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

    [SerializeField] bool _deactivateOnContact = false;
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
    public new Collider2D collider2D;
    AudioSource audioSource;
    PlayerAttack playerAttack;

    private static bool _guiSlidersEnabled = false;
    private static int _guiCount = 0;
    private int _guiIndex;

    /// <summary>
    /// the list of objects that have been already damaged in this hitbox session
    /// </summary>
    private readonly HashSet<Collider2D> _hitsInSession = new HashSet<Collider2D>();


    void OnEnable()
    {
        collider2D.enabled = true;
    }

    void OnDisable()
    {
//        collider2D.enabled = false;
        _hitsInSession.Clear(); // clear the list when ending the session
    }

    void Awake()
    {
        _guiIndex = _guiCount++;

        if (layerMask.value == 0)
            layerMask = LayerMask.GetMask("Enemy", "EnemyIgnore", "Object"); //value of 6144
        playerAttack = GetComponentInParent<PlayerAttack>();
        collider2D = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        collider2D.enabled = true;
    }

    void Start()
    {
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            collider2D.bounds.center,
            collider2D.bounds.size,
            angle: 0,
            layerMask: layerMask
        );
        foreach (Collider2D other in hits)
        {
            // skip self
            if (other.transform.root == this.transform.root)
                continue;

            if (_hitsInSession.Contains(other))
                break;

            _hitsInSession.Add(other);

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
            Vector2 toTarget = (other.transform.position - transform.parent.transform.position).normalized;
            attackDirection.x *= Mathf.Sign(toTarget.x);
            other.attachedRigidbody.AddForce(toTarget + attackDirection, ForceMode2D.Impulse);

            /**a multiplier that depends on the speed at which the attacker hit*/
            float speedMult = Mathf.Clamp(
                Mathf.Log( // Log() cuz we don't want to keep dealing more damage the faster you hit, there comes a point where it has to plateau
                    Mathf.Abs(Vector2.Dot(GameManager.PlayerRb.velocity,
                        attackDirection)) // abs() cuz no negatives are allowed in Log()
                ),
                1f, 50f);
            // safety check
            if (float.IsNaN(speedMult)) speedMult = 1f;
            if (playerAttack.CurrentComboInstance != null)
                speedMult = speedMult * Mathf.Log(playerAttack.CurrentComboInstance.Count);

//            Debug.Log("speedMult = " + speedMult);
            float nonzeroSpeedMult = 1 + speedMult;

            // do attack stuff
            if (otherHealth)
                otherHealth.TakeDamage(Mathf.RoundToInt(damageAmount * nonzeroSpeedMult), attackDirection);
            bool isFinalBlow = !wasDead && (otherHealth && otherHealth.IsDead);

            // invoke the hit event
            if (otherHealth && !otherHealth.IsDead)
                if (OnHitEvent != null)
                    OnHitEvent(other.gameObject, nonzeroSpeedMult, isFinalBlow);


            if (attackSound)
                audioSource.PlayOneShot(attackSound);

            if (hitStop > 0)
            {
                float seconds = hitStop * nonzeroSpeedMult;

                if (isFinalBlow) seconds *= killCoeff;
                GameManager.TimeManager.DoHitStop(seconds);
            }

            if (fisheye > 0)
            {
                GameManager.CameraController.DoFisheye(fisheye);
            }

            if (slomoFactor < 1)
            {
                float theSlowdownFactor = slomoFactor / nonzeroSpeedMult;
                if (isFinalBlow) theSlowdownFactor /= killCoeff;
                GameManager.TimeManager.DoSlowMotion(theSlowdownFactor);
            }

            if (_explosive)
                playerAttack.CreateSlamExplosion();

            GameManager.CameraShake.DoJitter(jitter.x * Mathf.Log(nonzeroSpeedMult), jitter.y);
        }

        if (_deactivateOnContact && otherHealth)
        {
            collider2D.enabled = false;
//            Debug.Log("Punch trigger disabled because it came in contact with " + other.gameObject.name);
        }
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 20, 80, 20), "Sliders")) _guiSlidersEnabled = !_guiSlidersEnabled;
        if (!_guiSlidersEnabled)
            return;

        hitStop = GameManager.AddGUISlider(gameObject.name + " hitStop", hitStop, 35 * (1 + _guiIndex));
        slomoFactor = GameManager.AddGUISlider(gameObject.name + " slomoFactor", slomoFactor,
            Mathf.RoundToInt(35 * (1.5f + _guiIndex)));
    }
}