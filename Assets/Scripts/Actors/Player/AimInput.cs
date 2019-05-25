using InControl;
using UnityEngine;
using UnityEngine.Serialization;

namespace Actors.Player
{
    [RequireComponent(typeof(Walker))]
    public class AimInput : Targeting
    {
        /// <summary>
        /// Do NOT modify this directly outside the class
        /// </summary>
        public bool UsingJoystick = false;
        public bool UsingMouse = false;

        [SerializeField] bool m_debugMousePosition = false;
        [SerializeField] float m_mouseMoveThreshold = 0.3f;
        [SerializeField] float m_joystickThreshold = 0.2f;

        private Vector2 m_lastMousePos;
        private Walker m_walker; // playerMove


        private void Awake()
        {
            m_walker = GameComponents.Player.GetComponent<Walker>();;
        }
    
        private void FixedUpdate()
        {
            RecheckInputDevice();
            m_lastMousePos = MousePos;
#if !(UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE)
            //switch to mouse if mouse pressed:
            if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
            {
                //Debug.Log("Mouse button pressed, switching to mouse control");
                UsingMouse = true;
                UsingJoystick = false;
            }
            if (Input.GetKey(KeyCode.LeftShift))
            {
                UsingMouse = false;
            }
            if (InputManager.ActiveDevice.AnyButton)
            {
                UsingMouse = false;
                UsingJoystick = true;
            }
#endif
        }

        /// Returns an aimDirection as a normalized Vector2
        public override Vector2 AimDirection
        {
            get
            {
                // Using joystick, if there is input
                Vector2 aimDir = RightJoystick;
                if (UsingJoystick && aimDir.magnitude > 0.1f)
                {
                    return (aimDir.normalized);
                }
            
                //Using mouse
                if (UsingMouse)
                {
                    return (MousePos - (Vector2)GameComponents.Player.transform.position).normalized;
                }
            
                // Using Movement aiming
                Vector2 moveInput = InputGetAxisRawVector;
                // If input is large enough
                return moveInput.magnitude > 0.1f 
                    ? moveInput.normalized 
                    : Vector2.right * m_walker.FacingSign;
            }
        }

        /// Checks which device should be used as aim input (Mouse or Joystick) and updates fields
        private void RecheckInputDevice()
        {
            float mouseDisp = Vector3.Distance(MousePos, m_lastMousePos);
            if (m_debugMousePosition) Debug.Log("mouseDisp: " + mouseDisp);

            if (!UsingMouse)
            {
                //Switch to mouse
                float camDisp = GameComponents.CameraController.LookAheadPos.magnitude;
                float compensatedMouseDisp = Mathf.Abs(mouseDisp) - Mathf.Abs(camDisp);
                // If mousespeed is greater than camSpeed (camera movement causes mouse to move, we want to compensate for this)
                if (compensatedMouseDisp > m_mouseMoveThreshold || Input.GetMouseButton(0))
                {
                    UsingMouse = true; Debug.Log("Switch control to using mouse");
                    UsingJoystick = false;
                }
            }
            if (!UsingJoystick)
            {
                // Switch to joystick, if there is enough input
                if (InputManager.ActiveDevice.AnyButton || RightJoystick.magnitude > m_joystickThreshold || LeftJoystick.magnitude > m_joystickThreshold)
                {
                    Debug.Log("Swiching to joystick");
                    UsingJoystick = true;
                    UsingMouse = false;
                }
            }
            //Use joystick by default
            if (UsingMouse && UsingJoystick)
            {
                Debug.Log("Switched to DEFAULT");
                UsingMouse = true;
                UsingJoystick = false;
            }
        }

        /// Returns mouse position as Vector2 in world space
        public static Vector2 MousePos
        {
            get
            {
                float depth;
                if (GameComponents.Player)
                    depth = GameComponents.Player.transform.position.z - Camera.main.transform.position.z;
                else
                    depth = Mathf.Abs(Camera.main.transform.position.z);

                return (Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth)));
            }
        }
        /// returns joystick right stick axies as a Vector2
        public static Vector2 RightJoystick
        {
            get
            {
                InputDevice inputDevice = InputManager.ActiveDevice;
                return new Vector2(inputDevice.RightStickX, inputDevice.RightStickY);
            }
        }

        /// Returns the left stick joystick axies as a Vector2
        public static Vector2 LeftJoystick
        {
            get
            {
                InputDevice inputDevice = InputManager.ActiveDevice;
                return new Vector2(inputDevice.LeftStickX, inputDevice.LeftStickY);
                //new Vector2(Input.GetAxisRaw("Horizontal_LS"), Input.GetAxisRaw("Vertical_LS"));
            }
        }

        /// Returns Vector2 of Input.GetAxis and Input.GetAxisRaw with Horizontal and Vertical depending on rawAxis
        public static Vector2 InputGetAxisVector => new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        public static Vector2 InputGetAxisRawVector => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
}
