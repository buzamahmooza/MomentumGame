using System.Collections;
using System.Collections.Generic;
using Actors;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Explosion : MonoBehaviour
{
    /// <summary> the layers that are safe </summary>
    [SerializeField] private LayerMask _safeLayer;

    /// <summary> the layers to do damage to </summary>
    [SerializeField] private LayerMask _damageLayer;

    [SerializeField] private int _damageAmount;
    [SerializeField] private float _forceAmount = 50f;

    public Explosion()
    {
        if (_damageLayer.value == 0)
            _damageLayer.value = -1;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Utils.IsInLayerMask(_damageLayer, other.gameObject.layer) &&
            !Utils.IsInLayerMask(_safeLayer, other.gameObject.layer))
        {
            Health otherHealth = other.gameObject.GetComponent<Health>();
            if (otherHealth != null)
            {
                otherHealth.TakeDamage(_damageAmount);
                Rigidbody2D otherRb = other.attachedRigidbody;
                if (otherRb != null)
                {
                    Vector3 toTarget = transform.position - other.transform.position;
                    otherRb.AddForce(toTarget.normalized / toTarget.magnitude*_forceAmount, ForceMode2D.Impulse);
                }
            }
        }
    }
}