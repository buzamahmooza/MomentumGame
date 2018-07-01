using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
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
    [SerializeField] float forceAmount = 7.0f;

    //components
    Collider2D trigger;
    AudioSource audioSource;
    PlayerAttack attackScript;


    private void Awake() {
        attackScript = GetComponentInParent<PlayerAttack>();
        trigger = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        trigger.enabled = false;
    }

    private void Start() {
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other) {
        var otherHealth = other.gameObject.GetComponent<Health>();
        if (other.gameObject.layer.CompareTo(LayerMask.NameToLayer("Ignore Raycast")) != 0 && other.attachedRigidbody) {
            Vector3 forceVec = other.transform.position - transform.parent.transform.position;
            other.attachedRigidbody.AddForce(forceAmount * (forceVec.normalized + Vector3.up * 0.7f), ForceMode2D.Impulse);

            //attackScript.DoAttackStuff(trigger, other);
            float speedMult = Mathf.Clamp(Mathf.Log(GameManager.PlayerRb.velocity.magnitude), 1f, 100);

            // attack stuff
            if (attackSound) audioSource.PlayOneShot(attackSound);
            if (otherHealth) otherHealth.TakeDamage(Mathf.RoundToInt(damageAmount * speedMult));
            if (hitStop > 0) GameManager.TimeManager.DoHitStop(hitStop * speedMult);
            if (fisheye > 0) GameManager.CameraController.DoFisheye(fisheye);
            if (slomoFactor < 1) {
                Debug.Log("DoSlowMotion:    " + slomoFactor);
                GameManager.TimeManager.DoSlowMotion(slomoFactor / speedMult);
            }

            if (explosive) attackScript.CreateSlamExplosion();

            GameManager.CameraShake.DoJitter(jitter.x * 0.5f * speedMult, jitter.y);

            //Debug.Log("Attacked " + other.gameObject.name);
        }

        if (deactivateOnContact && otherHealth) {
            trigger.enabled = false;
            Debug.Log("Punch trigger disabled because it came in contact with " + other.gameObject.name);
        }
    }

}