using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// assuming the rocket direction is facing right (the positive x axis)
/// </summary>
public class Missile : BulletScript
{
    public Transform Target;

    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _rotationSpeed = 1f;

    [SerializeField] private GameObject _particleEffects;
    [SerializeField] private AudioClip _explosionClip;
    [SerializeField] private float _explosionForce = 5;

    public bool IsArmed = true;
    
    private Rigidbody2D _rb;
    private GameObject _objectSpawnedIn;


    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        RaycastHit2D[] matches = Physics2D.CircleCastAll(transform.position, 0.5f, Vector2.up).Where(hit =>
            !hit.transform.IsChildOf(transform) && !hit.collider.isTrigger
        ).ToArray();
        if (matches.Length > 0)
        {
            _objectSpawnedIn = matches[0].collider.gameObject;
            if (_objectSpawnedIn)
            {
                print("Missile spawned in object: " + _objectSpawnedIn.name + ", dissarming");
                IsArmed = false;
                Invoke("Arm", 0.1f);
            }
        }
    }

    /// <summary>
    /// Arms the missile after a delay
    /// </summary>
    /// <returns></returns>
    public void Arm()
    {
        IsArmed = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Target != null)
        {
            Vector3 toTarget = Target.position - transform.position;
            float angle = Vector2.SignedAngle(transform.right, toTarget);
            _rb.angularVelocity = angle * _rotationSpeed;
        }

        _rb.velocity = transform.right * _speed;
    }

    protected override void OnTriggerEnter2D(Collider2D col)
    {
        if (col.isTrigger)
        {
            Debug.Log(name + " didn't explode cuz usedByEffector || isTrigger");
            return;
        }

        // don't collide with other bullets or missiles
        if (col.gameObject.GetComponent<BulletScript>())
        {
            Debug.Log(name + " didn't explode cuz " + col.name + " has BulletScript");
            return;
        }

        bool hitTarget = col.transform == Target;
        bool hitExplosionMask = Utils.IsInLayerMask(destroyMask, col.gameObject.layer);
        if (hitTarget || hitExplosionMask && IsArmed)
        {
            Debug.Log(string.Format(
                "Missile exploded: ({0})",
                hitTarget
                    ? $"target was hit: {col.name}"
                    : $"hit explosion mask ({LayerMask.LayerToName(col.gameObject.layer)})"
            ));
            
            Health otherHealth = col.GetComponent<Health>();
            if (otherHealth)
                otherHealth.TakeDamage(damageAmount, transform.rotation.eulerAngles.normalized);
            Explode();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == _objectSpawnedIn)
        {
            Debug.Log("Missile left object spawned in");
            IsArmed = true;
        }
    }

    void Explode()
    {
        Destroy(gameObject); // delay so that the audio will play
        if (_explosionClip)
            GameComponents.AudioSource.PlayOneShot(_explosionClip);

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