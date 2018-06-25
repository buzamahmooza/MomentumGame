using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet_Script : MonoBehaviour {

    public float speed = 50;
    Rigidbody2D rb;
    public float damageAmount = 5;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }
    void Start () {
        rb.velocity = transform.up*speed*Time.deltaTime;
	}

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.gameObject.CompareTag("Player") && other.gameObject.CompareTag("Shootable"))
        {
            if(other.gameObject.GetComponent<Object_Health_Script>() != null)
                other.gameObject.GetComponent<Object_Health_Script>().TakeDamage(damageAmount);
            Destroy(gameObject);
        }
    }

    void OnBecameInvisible() {
        Destroy(gameObject);
    }
}
