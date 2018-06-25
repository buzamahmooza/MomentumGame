using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class HitBox : MonoBehaviour 
{
    public float forceAmount = 5.0f;
    BoxCollider2D trigger;
    //[SerializeField]BoxCollider2D punchTrigger, slamTrigger;

    private void Awake(){
        //punchTrigger = GetComponents<BoxCollider2D>()[0];
        //slamTrigger = GetComponents<BoxCollider2D>()[1];
        //slamTrigger.enabled = punchTrigger.enabled = false;
        trigger = GetComponent<BoxCollider2D>();
        trigger.enabled = false;
    }
    
    void OnTriggerEnter2D(Collider2D other) {
        if (other.attachedRigidbody) {
            Vector3 forceVec = other.transform.position - transform.parent.transform.position;
            other.attachedRigidbody.AddForce(forceAmount * forceVec, ForceMode2D.Impulse);
            Debug.Log("Attacked " + other.gameObject.name);
        }
        //slamTrigger.enabled = punchTrigger.enabled = false;
        trigger.enabled = false;
    }
}
