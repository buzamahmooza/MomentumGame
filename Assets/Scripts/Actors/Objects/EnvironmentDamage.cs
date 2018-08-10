using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentDamage : MonoBehaviour
{
    private AudioSource _audioSource;
    [SerializeField] private int _damageAmount = 25;
    [SerializeField] private GameObject _collisionParticles;
    new Collider2D collider2D;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        collider2D = GetComponent<Collider2D>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.isTrigger) return;
        _audioSource.Play();

        Health otherHealth = other.GetComponent<Health>();
        if (otherHealth)
        {
            otherHealth.TakeDamage(_damageAmount);
        }

        // the position of the other object relative to this object
        Vector3 offset = other.transform.position - this.transform.position;

        if (other.attachedRigidbody)
        {
            other.attachedRigidbody.AddForce(offset.normalized * other.attachedRigidbody.mass * 10,
                ForceMode2D.Impulse);
        }

        if (_collisionParticles != null)
        {
            Instantiate(
                _collisionParticles,
                transform.position + offset.normalized * collider2D.bounds.extents.magnitude,
                Quaternion.identity
            );
        }
    }

}