using System;
using System.Linq;
using UnityEngine;

public class BulletScript : MonoBehaviour
{

    [SerializeField] public int damageAmount = 2;
    [SerializeField] private LayerMask destroyMask;

    private Rigidbody2D rb;

    //private float reflectDist;
    //[SerializeField] private LayerMask reflectionLayer;

    private GameObject parent;
    public bool damageShootersWithSameTag = false;  // should the bullet also damage other game objects with the same tag as the shooter (parent)?


    public void CorrectRotation() {
        transform.Rotate(Vector3.up, 90);
        transform.Rotate(Vector3.forward, 90);
        //rb.velocity = transform.TransformDirection(Vector3.forward * 10);
    }

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, 7);
    }

    private void Start() {
        CorrectRotation();
        if (!transform.parent)
            return;

        parent = transform.parent.gameObject;
        //print("parent is " + parent.name);
        transform.parent = null; // detatch from parent
        //rb.velocity = transform.up*speed*Time.deltaTime;
        //reflectDist = GetComponent<SpriteRenderer>().size.y / 2.0f;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject == parent) // prevent damaging the shooter
            return;

        Health otherHealth = other.gameObject.GetComponent<Health>();
        if (otherHealth) {
            // if it's the same type as the shooter, do damage
            if (!other.gameObject.CompareTag(parent.tag) || damageShootersWithSameTag)
                otherHealth.TakeDamage(damageAmount);
        }

        if (other.gameObject.layer == destroyMask.value) {
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible() {
        Destroy(gameObject, 3f);
    }

}
