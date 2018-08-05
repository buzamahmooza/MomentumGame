using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// assuming the rocket direction is facing right (the positive x axis)
/// </summary>
public class Missile : BulletScript
{
    public Transform Target;
    private Rigidbody2D _rb;
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _rotationSpeed = 1f;

    [SerializeField] private GameObject _particleEffects;
    [SerializeField] private AudioClip _explosionClip;
    [SerializeField] private float _explosionForce = 5;


    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Target)
        {
            _rb.velocity = transform.right * _speed;

            var toTarget = Target.position - transform.position;
            var angle = Vector2.SignedAngle(transform.right, toTarget);
            _rb.angularVelocity = angle * _rotationSpeed;
        }
    }

    protected new void OnTriggerEnter2D(Collider2D other)
    {
        if (other.usedByEffector)
            return;
        if (other.transform == Target || Utils.IsInLayerMask(destroyMask, other.gameObject.layer))
        {
            Health otherHealth = other.GetComponent<Health>();
            if (otherHealth)
                otherHealth.TakeDamage(damageAmount, transform.rotation.eulerAngles.normalized);
            Explode();
        }
    }

    private void Explode()
    {
        Destroy(gameObject); // delay so that the audio will play
        if (_explosionClip) GameManager.AudioSource.PlayOneShot(_explosionClip);
        if (_particleEffects)
            Instantiate(_particleEffects, transform.position, transform.rotation);

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, 1, destroyMask))
        {
            Rigidbody2D otherRb = hit.attachedRigidbody;
            float distance = Vector2.Distance(transform.position, hit.gameObject.transform.position);
            if (otherRb)
                otherRb.AddForce(
                    _explosionForce * (transform.position - hit.transform.position).normalized / (1 + distance) * 5,
                    ForceMode2D.Impulse
                );

            var otherHealth = hit.gameObject.GetComponent<Health>();
            if (!otherHealth)
            {
                Debug.LogWarning("Health not found on " + hit.name);
                continue;
            }

            otherHealth.Stun(2);
            otherHealth.TakeDamage(damageAmount);
        }
    }
}