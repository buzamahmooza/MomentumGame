using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

[RequireComponent(typeof(PlayerMove))]
public class AimInput : Targeting
{
    public float mouseMoveThreshold = 0.3f,
        JoystickThreshold = 0.2f;
    public bool debugMousePosition = false;
    /// <summary>
    /// Do NOT modify this directly outside the class
    /// </summary>
    [SerializeField] public bool usingJoystick = false;
    [SerializeField] public bool usingMouse = false;
    private Vector2 lastMousePos;
    private PlayerMove playerMove;


    private void Awake() {
        playerMove = GameManager.Player.GetComponent<PlayerMove>();
    }

    private void FixedUpdate() {
        RecheckInputDevice();
        lastMousePos = MousePos;

        //switch to mouse if mouse pressed:
        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1)) {
            //Debug.Log("Mouse button pressed, switching to mouse control");
            usingMouse = true;
            usingJoystick = false;
        }
        if (Input.GetKey(KeyCode.LeftShift)) {
            usingMouse = false;
        }
        if (InputManager.ActiveDevice.AnyButton) {
            usingMouse = false;
            usingJoystick = true;
        }
    }

    /// <summary>
    /// Returns an aimDirection as a normalized Vector2
    /// </summary>
    /// <returns>Returns an aimDirection as a normalized Vector2</returns>
    public override Vector2 AimDirection {
        get {
            // Using joystick, if there is input
            Vector2 aimDir = RightJoystick;
            if (usingJoystick && Mathf.Abs(aimDir.magnitude) > 0.1f) {
                // ~Debug.Log("Using controller aiming, aimDirection = " + aimDir);
                return (aimDir.normalized);
            }

            //Using mouse
            else if (usingMouse) {
                return (MousePos - (Vector2)GameManager.Player.transform.position).normalized;
            }
            // Using Movement aiming
            else {
                Vector2 moveInput = InputGetAxisRawVector;

                // If input is large enough
                if (Mathf.Abs(moveInput.magnitude) > 0.1f) {
                    return (moveInput.normalized); //Normalized vector
                }
                // If input is too little, just aim where player is facing
                else {
                    return (Vector2.right * playerMove.FacingSign);
                }
            }
        }
    }

    /// <summary>
    /// Checks which device should be used as aim input (Mouse or Joystick) and updates fields
    /// </summary>
    private void RecheckInputDevice() {
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
        if (!usingJoystick) {
            // Switch to joystick, if there is enough input
            if (InputManager.ActiveDevice.AnyButton || RightJoystick.magnitude > JoystickThreshold || LeftJoystick.magnitude > JoystickThreshold) {
                Debug.Log("Swiching to joystick");
                usingJoystick = true;
                usingMouse = false;
            }
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
    public static Vector2 RightJoystick {
        get {
            var inputDevice = InputManager.ActiveDevice;
            return new Vector2(inputDevice.RightStickX, inputDevice.RightStickY);
        }
    }

    /// <summary>
    /// Returns the left stick joystick axies as a Vector2
    /// </summary>
    /// <returns></returns>
    public static Vector2 LeftJoystick {
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
    public static Vector2 InputGetAxisVector {
        get {
            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }
    }
    public static Vector2 InputGetAxisRawVector {
        get {
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
    }
}
