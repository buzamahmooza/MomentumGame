using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] [Range(0, 1000)] public int damageAmount = 20;
    [SerializeField] private AudioClip hitSound;
    [HideInInspector] public new Collider2D collider2D;
    private AudioSource audioSource;
    private void OnEnable() { collider2D.enabled = true; }
    private void OnDisable() { collider2D.enabled = false; }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = GetComponentInParent<AudioSource>();
        collider2D = GetComponent<Collider2D>();
        collider2D.enabled = false;
    }
    private void Start()
    {
        if (!transform.parent)
            return;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (transform.IsChildOf(other.gameObject.transform)) // prevent damaging the attacker
            return;

        Health otherHealth = other.gameObject.GetComponent<Health>();
        // if it's NOT the same type as the attacker, do damage (so enemies can't damage eachother)
        if (otherHealth && !other.CompareTag(transform.parent.tag))
        {
            otherHealth.TakeDamage(damageAmount);
            if (hitSound) audioSource.PlayOneShot(hitSound);
        }
    }
}
