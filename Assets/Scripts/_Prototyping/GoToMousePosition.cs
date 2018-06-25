using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToMousePosition : MonoBehaviour {
    private void Update () {
        transform.position = AimInput.MousePos;
	}
}
