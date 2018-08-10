using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script that just rotates the object forever
/// </summary>
public class RotatorMotor : MonoBehaviour
{
    [SerializeField]
    private float _degreesPerSecond = 10;
    [SerializeField] private Vector3 _axis = Vector3.forward;

    private void LateUpdate()
    {
        transform.Rotate(_axis, _degreesPerSecond * Time.deltaTime);
    }
}
