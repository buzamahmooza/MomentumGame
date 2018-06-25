using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerHitbox : MonoBehaviour
{
    public float forceAmount = 7.0f;
    private BoxCollider2D trigger;
    private AudioSource audioSource;
    private PlayerAttack attackScript;
    public bool shouldDeactivateOnContact = false;
    public Vector2 jitter = new Vector2(.2f, .09f);
    public bool debug = false;

    private void Awake() {
        attackScript = transform.GetComponentInParent<PlayerAttack>();
        audioSource = GetComponent<AudioSource>();
        trigger = gameObject.GetComponent<BoxCollider2D>();
        trigger.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.attachedRigidbody) {
            Vector3 forceVec = other.transform.position - transform.parent.transform.position;
            other.attachedRigidbody.AddForce(forceAmount * (forceVec.normalized + Vector3.up*0.7f), ForceMode2D.Impulse);
            attackScript.DoAttackStuff(trigger, other);
            GameManager.CameraShake.DoJitter(jitter.x * 0.5f * GameManager.Player.GetComponent<Rigidbody2D>().velocity.magnitude, jitter.y);
            if (debug) Debug.Log("Attacked " + other.gameObject.name);
        }
        if (shouldDeactivateOnContact && other.GetComponent<HealthScript>()) {
            trigger.enabled = false;
            Debug.Log("Punch trigger disabled because it came in contact with " + other.gameObject.name);
        }
    }
}