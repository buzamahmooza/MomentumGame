using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Shoot_Script : MonoBehaviour {

    public float timeBetweenShots = 0.1f;
    public Transform shootTransform;
    public GameObject bullet;

    private float timePassed = 0;
    
	// Update is called once per frame
	void Update () {
        timePassed += Time.deltaTime;
        if (Input.GetButton("Fire2") || Input.GetKey(KeyCode.RightShift))
            Fire();
	}

    private void Fire() {
        Debug.Log("TimePassed: " + timePassed);
        if (timePassed >= timeBetweenShots) {
            Instantiate(bullet, shootTransform.position,shootTransform.rotation);
            timePassed = 0;
        }
    }
}
