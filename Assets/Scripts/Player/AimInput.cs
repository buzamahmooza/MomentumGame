using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

[RequireComponent(typeof(PlayerMove))]
public class AimInput : MonoBehaviour
{

    public GameObject arrow;
    public float mouseMoveThreshold = 0.3f,
        JoystickThreshold = 0.2f;
    public bool debugMousePosition = false;
    [SerializeField] private bool usingJoystick = false;
    [SerializeField] private bool usingMouse = false;
    private Vector2 lastMousePos;
    private PlayerMove playerMove;


    private void Start() {
        playerMove = GameManager.Player.GetComponent<PlayerMove>();
    }

    private void FixedUpdate() {
        CheckInputDevice();
        lastMousePos = MousePos;

        //switch to mouse if mouse pressed:
        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1)) {
            Debug.Log("Mouse button pressed, switching to mouse control");
            usingMouse = true;
            usingJoystick = false;
        }

        //Debug.Log("AimDirection: " + AimDirection);
    }


    public Vector2 AimDirection {
        get {
            Vector2 aimDir = new Vector2();

            // Using joystick, if there is input
            aimDir = RightJoystick;
            if (usingJoystick && Mathf.Abs(aimDir.magnitude) > 0.1f) {
                // ~Debug.Log("Using controller aiming, aimDirection = " + aimDir);
                return (aimDir.normalized);
            }

            //Using mouse
            else if (usingMouse) {
                Vector2 returnVector = MousePos - (Vector2)GameManager.Player.transform.position;
                return returnVector.normalized;
            }
            // Using Movement aiming
            else {
                //Vector2 moveInput = InputGetAxisVector;
                Vector2 moveInput = InputGetAxisRawVector;

                // If input is large enough
                if (Mathf.Abs(moveInput.magnitude) > 0.1f) {
                    //Debug.Log("Using moveInput, moveInput is LARGER than 0.1");
                    return (moveInput.normalized); //Normalized vector
                }
                // If input is too little, just aim where player is facing
                else {
                    //Debug.Log("Using facingDir, moveInput is SMALLER than 0.1");
                    return (Vector2.right * playerMove.FacingRightSign);
                }
            }
        }
    }


    /// <summary>
    /// Checks which device should be used as aim input (Mouse or Joystick) and updates fields
    /// </summary>
    private void CheckInputDevice() {

        float mouseDisp = Vector3.Distance(MousePos, lastMousePos);
        if (debugMousePosition) Debug.Log("mouseDisp: " + mouseDisp);

        if (!usingMouse) {
            //Switch to mouse
            float camDisp = GameManager.CameraController.LookAheadPos.magnitude;
            float compensatedMouseDisp = Mathf.Abs(mouseDisp) - Mathf.Abs(camDisp);
            // If mousespeed is greater than camSpeed (camera movement causes mouse to move, we want to compensate for this)
            if (compensatedMouseDisp > mouseMoveThreshold || Input.GetMouseButton(0)) {
                usingMouse = true; Debug.Log("Switch control to using mouse");
                usingJoystick = false;
            }
        }
        if (!usingJoystick)
            // Switch to joystick, if there is enough input
            if (RightJoystick.magnitude > JoystickThreshold || LeftJoystick.magnitude > JoystickThreshold) {
                Debug.Log("Swiching to joystick");
                usingJoystick = true;
                usingMouse = false;
            }
        //Use joystick by default
        if (usingMouse && usingJoystick) {
            Debug.Log("Switched to DEFAULT");
            usingMouse = true;
            usingJoystick = false;
        }
    }


    /// <summary>
    /// Returns mouse position as Vector2 in world space
    /// </summary>
    /// <returns>Returns mouse position as Vector2 in world space</returns>
    public static Vector2 MousePos {
        get {
            Camera c = Camera.main;
            float depth;
            if (GameManager.Player)
                depth = GameManager.Player.transform.position.z - Camera.main.transform.position.z;
            else
                depth = Mathf.Abs(c.transform.position.z);

            return (c.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth)));
        }
    }
    /// <summary>
    /// returns joystick right stick axies as a Vector2
    /// </summary>
    /// <returns></returns>
    private Vector2 RightJoystick {
        get {
            var inputDevice = InputManager.ActiveDevice;
            return new Vector2(inputDevice.RightStickX, inputDevice.RightStickY);
            //return (new Vector2(Input.GetAxisRaw("HorizontalTurn"), Input.GetAxisRaw("VerticalTurn")));
        }
    }

    /// <summary>
    /// Returns the left stick joystick axies as a Vector2
    /// </summary>
    /// <returns></returns>
    private Vector2 LeftJoystick {
        get {
            var inputDevice = InputManager.ActiveDevice;
            return new Vector2(inputDevice.LeftStickX, inputDevice.LeftStickY);
            //new Vector2(Input.GetAxisRaw("Horizontal_LS"), Input.GetAxisRaw("Vertical_LS"));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rawAxis"></param>
    /// <returns>Vector2 of Input.GetAxis and Input.GetAxisRaw with Horizontal and Vertical depending on rawAxis</returns>
    private Vector2 InputGetAxisVector {
        get {
            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }
    }
    private Vector2 InputGetAxisRawVector {
        get {
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
    }
}
