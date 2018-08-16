using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentDamage : MonoBehaviour
{
    [SerializeField] private int _damageAmount = 25;
    [SerializeField] private GameObject _collisionParticles;
    
    private AudioSource m_audioSource;
    private Collider2D m_collider2D;

    void Awake()
    {
        m_audioSource = GetComponent<AudioSource>();
        m_collider2D = GetComponent<Collider2D>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.isTrigger) return;
        m_audioSource.Play();

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
                transform.position + offset.normalized * m_collider2D.bounds.extents.magnitude,
                Quaternion.identity
            );
        }
    }

}