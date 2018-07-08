using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    /// <summary>
    /// An event which is called when the hitbox collides<br/>
    /// <param name="otherGameObject" type="GameObject"></param><br/>
    /// <param name="speedMult" type="float">the gameObject that was hit</param><br/>
    /// <param name="killedOther" type="bool">if this hit killed the other gameObject</param><br/>
    /// </summary>
    [HideInInspector] public event System.Action<GameObject, float, bool> OnHitEvent;

    // assigned in inspector
    [SerializeField] Vector2 jitter = new Vector2(0.2f, 0.09f);
    [SerializeField] AudioClip attackSound;
    [SerializeField] [Range(1, 1000)] int damageAmount = 25;

    [SerializeField] bool deactivateOnContact = true;
    [SerializeField] bool explosive = false;

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
    PlayerAttack attackScript;

    private static bool guiSlidersEnabled = false;
    private static int guiCount = 0;
    private int guiIndex;

    void OnEnable() { collider2D.enabled = true; }
    void OnDisable() { collider2D.enabled = false; }

    private void Awake() {
        guiIndex = guiCount++;

        if (layerMask.value == 0) // initialize to "Enemy", "EnemyIgnore", "Object" layers
            layerMask.value = 6144;
        attackScript = GetComponentInParent<PlayerAttack>();
        collider2D = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        collider2D.enabled = false;
    }

    private void Start() {
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other) {
        var otherHealth = other.gameObject.GetComponent<Health>();
        var isInLayerMask = (layerMask.value & 1 << other.gameObject.layer) != 0;
        Debug.Log("layermask.value=" + layerMask.value);

        bool wasDead = otherHealth && otherHealth.IsDead;

        if (isInLayerMask && other.attachedRigidbody) {

            Vector2 toTarget = (other.transform.position - transform.parent.transform.position).normalized;
            attackDirection.x *= Mathf.Sign(toTarget.x);
            other.attachedRigidbody.AddForce(toTarget + attackDirection, ForceMode2D.Impulse);

            float speedMult = Mathf.Clamp(
                Mathf.Log( // Log() cuz we don't want to keep dealing more damage the faster you hit, there comes a point where it has to plateau
                    Mathf.Abs(Vector2.Dot(GameManager.PlayerRb.velocity, attackDirection)) // abs() cuz no negatives are allowed in Log()
                ),
            1f, 50f);
            if (float.IsNaN(speedMult))
                speedMult = 1f;
            Debug.Log("speedMult = " + speedMult);

            // attack stuff
            if (otherHealth) otherHealth.TakeDamage(Mathf.RoundToInt(damageAmount * speedMult));
            bool killedOther = !wasDead && otherHealth.IsDead;

            if (attackSound) audioSource.PlayOneShot(attackSound);
            if (hitStop > 0) {
                var seconds = hitStop * speedMult;

                if (killedOther) seconds *= killCoeff;
                GameManager.TimeManager.DoHitStop(seconds);
            }

            if (fisheye > 0) {
                GameManager.CameraController.DoFisheye(fisheye);
            }

            if (slomoFactor < 1) {
                var theSlowdownFactor = slomoFactor / speedMult;
                if (killedOther) theSlowdownFactor /= killCoeff;
                GameManager.TimeManager.DoSlowMotion(theSlowdownFactor);
            }

            if (explosive) attackScript.CreateSlamExplosion();

            GameManager.CameraShake.DoJitter(jitter.x * speedMult, jitter.y);

            // invoke the hit event
            if (OnHitEvent != null)
                OnHitEvent(other.gameObject, speedMult, killedOther);
        }

        if (deactivateOnContact && otherHealth) {
            collider2D.enabled = false;
            Debug.Log("Punch trigger disabled because it came in contact with " + other.gameObject.name);
        }
    }

    void OnGUI() {
        if (GUI.Button(new Rect(100, 20, 80, 20), "Sliders")) guiSlidersEnabled = !guiSlidersEnabled;
        if (!guiSlidersEnabled)
            return;

        hitStop = GameManager.AddGUISlider(gameObject.name + " hitStop", hitStop, 35 * (1 + guiIndex));
        slomoFactor = GameManager.AddGUISlider(gameObject.name + " slomoFactor", slomoFactor, Mathf.RoundToInt(35 * (1.5f + guiIndex)));
    }

}