using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    private bool _canExplode = true;


    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        _canExplode = false;
        StartCoroutine(AllowExplodingDelayed());
    }

    private IEnumerator AllowExplodingDelayed()
    {
        yield return new WaitForSeconds(0.7f);
        _canExplode = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Target == null)
        {
            Explode();
        }
        else
        {
            _rb.velocity = transform.right * _speed;

            Vector3 toTarget = Target.position - transform.position;
            float angle = Vector2.SignedAngle(transform.right, toTarget);
            _rb.angularVelocity = angle * _rotationSpeed;
        }
    }

    protected new void OnTriggerEnter2D(Collider2D other)
    {
        if (other.usedByEffector || other.isTrigger)
            return;

        if (other.gameObject.CompareTag(this.tag)) // don't collide with other bullets or missiles
            return;

        if (other.transform == Target || Utils.IsInLayerMask(destroyMask, other.gameObject.layer) && _canExplode)
        {
            Health otherHealth = other.GetComponent<Health>();
            if (otherHealth)
                otherHealth.TakeDamage(damageAmount, transform.rotation.eulerAngles.normalized);

            Explode();
        }
    }

    void Explode()
    {
        Destroy(gameObject); // delay so that the audio will play
        if (_explosionClip)
            GameManager.AudioSource.PlayOneShot(_explosionClip);
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

            Health otherHealth = hit.gameObject.GetComponent<Health>();
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