using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeZoneDestroyer : MonoBehaviour {

	// Use this for initialization
    private void Start () {
		
	}
	
	// Update is called once per frame
    private void Update () {
		
	}

    private void OnTriggerExit2D(Collider2D collision) {
        Destroy(collision.gameObject);
        print("Object left the safe zone, destroying object: " + collision.gameObject.name);
    }
}
