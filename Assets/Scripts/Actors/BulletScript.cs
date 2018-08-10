using System;
using System.Linq;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [SerializeField] public int damageAmount = 2;
    [SerializeField] protected LayerMask destroyMask;

    /// <summary> the object that created this bullet, useful for not damaging itself </summary>
    [HideInInspector] public GameObject Shooter;

    /// should the bullet also damage other game objects with the same tag as the shooter (parent)?
    public bool damageShootersWithSameTag = false;

    [SerializeField] private bool _correctRotation;


    private void Awake()
    {
        if(destroyMask.value == 0) destroyMask = LayerMask.GetMask("Everything");
        Destroy(gameObject, 7);
    }

    public void CorrectRotation()
    {
        transform.Rotate(Vector3.up, 90);
        transform.Rotate(Vector3.forward, 90);
    }

    private void Start()
    {
        if (_correctRotation)
            CorrectRotation();
    }

    protected void OnTriggerEnter2D(Collider2D other)
    {
        // prevent damaging the attacker
        if (transform.IsChildOf(other.gameObject.transform))
            return;

        Health otherHealth = other.gameObject.GetComponent<Health>();
        if (otherHealth && other.gameObject)
        {
            // if it's the same type as the shooter, do damage
            if (Shooter == null || !other.gameObject.CompareTag(Shooter.tag) || damageShootersWithSameTag)
                otherHealth.TakeDamage(damageAmount, transform.rotation.eulerAngles.normalized);
        }

        if (Utils.IsInLayerMask(destroyMask, other.gameObject.layer))
        {
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject, 3f);
    }
}